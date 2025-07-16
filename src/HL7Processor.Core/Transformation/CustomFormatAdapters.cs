using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using HL7Processor.Core.Parsing;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;

namespace HL7Processor.Core.Transformation;

public class FhirFormatAdapter : IFormatAdapter<FhirBundle>
{
    private readonly ILogger<FhirFormatAdapter> _logger;

    public FhirFormatAdapter(ILogger<FhirFormatAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string AdapterName => "FHIR R4 Adapter";
    public string Description => "Converts HL7 v2 messages to FHIR R4 Bundle format";
    public string SupportedVersion => "R4";

    public bool CanConvertFrom(string data)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(data);
            return json.TryGetProperty("resourceType", out var resourceType) && 
                   resourceType.GetString() == "Bundle";
        }
        catch
        {
            return false;
        }
    }

    public bool CanConvertTo(HL7Message message)
    {
        return message.MessageType == HL7MessageType.ADT_A01 || 
               message.MessageType == HL7MessageType.ADT_A04 || 
               message.MessageType == HL7MessageType.ADT_A08 ||
               message.MessageType == HL7MessageType.ORU_R01;
    }

    public FhirBundle ConvertFrom(HL7Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var bundle = new FhirBundle
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = message.Timestamp,
            Type = "message"
        };

        // Create MessageHeader resource
        var messageHeader = new FhirResource
        {
            ResourceType = "MessageHeader",
            Id = message.MessageControlId ?? Guid.NewGuid().ToString()
        };

        messageHeader.Properties["eventCoding"] = new
        {
            system = "http://terminology.hl7.org/CodeSystem/v2-0003",
            code = message.MessageType.GetMessageTypeCode(),
            display = message.MessageType.GetDescription()
        };

        messageHeader.Properties["source"] = new
        {
            name = message.SendingApplication,
            software = message.SendingFacility,
            version = message.Version,
            endpoint = $"urn:{message.SendingApplication}"
        };

        messageHeader.Properties["destination"] = new[]
        {
            new
            {
                name = message.ReceivingApplication,
                endpoint = $"urn:{message.ReceivingApplication}"
            }
        };

        bundle.Resources.Add(messageHeader);

        // Create Patient resource if PID segment exists
        if (message.HasSegment("PID"))
        {
            var patient = CreatePatientResource(message);
            bundle.Resources.Add(patient);
            
            // Reference patient in MessageHeader
            messageHeader.Properties["focus"] = new[] { new { reference = $"Patient/{patient.Id}" } };
        }

        // Create Encounter resource if PV1 segment exists
        if (message.HasSegment("PV1"))
        {
            var encounter = CreateEncounterResource(message);
            bundle.Resources.Add(encounter);
        }

        // Create Observation resources if OBX segments exist
        var observations = CreateObservationResources(message);
        bundle.Resources.AddRange(observations);

        _logger.LogInformation("Successfully converted HL7 message {MessageId} to FHIR bundle", message.Id);
        return bundle;
    }

    public HL7Message ConvertTo(FhirBundle data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var message = new HL7Message
        {
            Id = data.Id,
            Timestamp = data.Timestamp,
            Version = "2.5"
        };

        // Find MessageHeader resource
        var messageHeader = data.Resources.FirstOrDefault(r => r.ResourceType == "MessageHeader");
        if (messageHeader != null)
        {
            if (messageHeader.Properties.TryGetValue("eventCoding", out var eventCoding))
            {
                var eventObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(eventCoding));
                if (eventObj.TryGetProperty("code", out var code))
                {
                    message.MessageType = HL7MessageTypeExtensions.FromMessageTypeCode(code.GetString());
                }
            }

            message.MessageControlId = messageHeader.Id;
        }

        // Create MSH segment
        var mshSegment = CreateMSHSegment(message, data);
        message.AddSegment(mshSegment);

        // Create PID segment from Patient resource
        var patient = data.Resources.FirstOrDefault(r => r.ResourceType == "Patient");
        if (patient != null)
        {
            var pidSegment = CreatePIDSegment(patient);
            message.AddSegment(pidSegment);
        }

        // Create PV1 segment from Encounter resource
        var encounter = data.Resources.FirstOrDefault(r => r.ResourceType == "Encounter");
        if (encounter != null)
        {
            var pv1Segment = CreatePV1Segment(encounter);
            message.AddSegment(pv1Segment);
        }

        // Create OBX segments from Observation resources
        var observations = data.Resources.Where(r => r.ResourceType == "Observation");
        foreach (var observation in observations)
        {
            var obxSegment = CreateOBXSegment(observation);
            message.AddSegment(obxSegment);
        }

        // Rebuild raw message
        message.RawMessage = string.Join("\r", message.Segments.Select(s => s.RawData));

        _logger.LogInformation("Successfully converted FHIR bundle to HL7 message {MessageId}", message.Id);
        return message;
    }

    public ValidationResult ValidateData(FhirBundle data)
    {
        var result = new ValidationResult { IsValid = true };

        if (data == null)
        {
            result.AddError("FHIR bundle cannot be null");
            return result;
        }

        if (string.IsNullOrEmpty(data.Id))
        {
            result.AddError("FHIR bundle must have an ID");
        }

        if (data.Type != "message")
        {
            result.AddWarning("FHIR bundle type should be 'message' for HL7 v2 conversion");
        }

        var messageHeader = data.Resources.FirstOrDefault(r => r.ResourceType == "MessageHeader");
        if (messageHeader == null)
        {
            result.AddError("FHIR bundle must contain a MessageHeader resource");
        }

        result.AddMetadata("ResourceCount", data.Resources.Count);
        result.AddMetadata("HasPatient", data.Resources.Any(r => r.ResourceType == "Patient"));
        result.AddMetadata("HasEncounter", data.Resources.Any(r => r.ResourceType == "Encounter"));

        return result;
    }

    private FhirResource CreatePatientResource(HL7Message message)
    {
        var patient = new FhirResource
        {
            ResourceType = "Patient",
            Id = message.GetPatientId()
        };

        // Name
        var patientName = message.GetPatientName();
        if (!string.IsNullOrEmpty(patientName))
        {
            var nameParts = patientName.Split(' ');
            patient.Properties["name"] = new[]
            {
                new
                {
                    use = "official",
                    given = nameParts.Take(nameParts.Length - 1).ToArray(),
                    family = nameParts.LastOrDefault()
                }
            };
        }

        // Gender
        var gender = message.GetPatientGender();
        if (!string.IsNullOrEmpty(gender))
        {
            patient.Properties["gender"] = gender.ToLowerInvariant() switch
            {
                "m" => "male",
                "f" => "female",
                "o" => "other",
                _ => "unknown"
            };
        }

        // Birth Date
        var birthDate = message.GetPatientBirthDate();
        if (birthDate.HasValue)
        {
            patient.Properties["birthDate"] = birthDate.Value.ToString("yyyy-MM-dd");
        }

        // Identifier
        patient.Properties["identifier"] = new[]
        {
            new
            {
                use = "usual",
                system = "http://hospital.example.com/patient-id",
                value = message.GetPatientId()
            }
        };

        return patient;
    }

    private FhirResource CreateEncounterResource(HL7Message message)
    {
        var encounter = new FhirResource
        {
            ResourceType = "Encounter",
            Id = message.GetVisitNumber()
        };

        encounter.Properties["status"] = "in-progress";
        encounter.Properties["class"] = new
        {
            system = "http://terminology.hl7.org/CodeSystem/v3-ActCode",
            code = message.GetPatientClass().ToLowerInvariant() switch
            {
                "i" => "IMP",
                "o" => "AMB",
                "e" => "EMER",
                _ => "AMB"
            }
        };

        // Subject reference
        encounter.Properties["subject"] = new
        {
            reference = $"Patient/{message.GetPatientId()}"
        };

        // Period
        var eventDateTime = message.GetEventDateTime();
        if (eventDateTime.HasValue)
        {
            encounter.Properties["period"] = new
            {
                start = eventDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        return encounter;
    }

    private List<FhirResource> CreateObservationResources(HL7Message message)
    {
        var observations = new List<FhirResource>();
        var obxSegments = message.GetSegments("OBX");

        foreach (var obxSegment in obxSegments)
        {
            var observation = new FhirResource
            {
                ResourceType = "Observation",
                Id = Guid.NewGuid().ToString()
            };

            observation.Properties["status"] = "final";
            observation.Properties["subject"] = new
            {
                reference = $"Patient/{message.GetPatientId()}"
            };

            // Code
            var observationId = obxSegment.GetFieldValue(3);
            if (!string.IsNullOrEmpty(observationId))
            {
                observation.Properties["code"] = new
                {
                    coding = new[]
                    {
                        new
                        {
                            system = "http://loinc.org",
                            code = observationId
                        }
                    }
                };
            }

            // Value
            var observationValue = obxSegment.GetFieldValue(5);
            if (!string.IsNullOrEmpty(observationValue))
            {
                observation.Properties["valueString"] = observationValue;
            }

            observations.Add(observation);
        }

        return observations;
    }

    private HL7Segment CreateMSHSegment(HL7Message message, FhirBundle bundle)
    {
        var msh = new HL7Segment("MSH", string.Empty);
        msh.SetFieldValue(1, "|");
        msh.SetFieldValue(2, "^~\\&");
        msh.SetFieldValue(3, "FHIR_ADAPTER");
        msh.SetFieldValue(4, "HOSPITAL");
        msh.SetFieldValue(5, "RECEIVING_APP");
        msh.SetFieldValue(6, "RECEIVING_FAC");
        msh.SetFieldValue(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
        msh.SetFieldValue(9, message.MessageType.GetMessageTypeCode());
        msh.SetFieldValue(10, message.MessageControlId ?? Guid.NewGuid().ToString());
        msh.SetFieldValue(11, "P");
        msh.SetFieldValue(12, "2.5");
        msh.RebuildRawData();
        return msh;
    }

    private HL7Segment CreatePIDSegment(FhirResource patient)
    {
        var pid = new HL7Segment("PID", string.Empty);
        pid.SetFieldValue(1, "1");
        pid.SetFieldValue(3, patient.Id);
        
        // Name
        if (patient.Properties.TryGetValue("name", out var nameObj))
        {
            var nameArray = JsonSerializer.Deserialize<JsonElement[]>(JsonSerializer.Serialize(nameObj));
            if (nameArray?.Length > 0)
            {
                var name = nameArray[0];
                var family = name.TryGetProperty("family", out var familyProp) ? familyProp.GetString() : "";
                var given = name.TryGetProperty("given", out var givenProp) ? 
                    string.Join(" ", givenProp.EnumerateArray().Select(g => g.GetString())) : "";
                pid.SetFieldValue(5, $"{family}^{given}");
            }
        }

        // Birth Date
        if (patient.Properties.TryGetValue("birthDate", out var birthDateObj))
        {
            var birthDate = birthDateObj.ToString();
            pid.SetFieldValue(7, birthDate?.Replace("-", ""));
        }

        // Gender
        if (patient.Properties.TryGetValue("gender", out var genderObj))
        {
            var gender = genderObj.ToString()?.ToUpperInvariant();
            pid.SetFieldValue(8, gender switch
            {
                "MALE" => "M",
                "FEMALE" => "F",
                "OTHER" => "O",
                _ => "U"
            });
        }

        pid.RebuildRawData();
        return pid;
    }

    private HL7Segment CreatePV1Segment(FhirResource encounter)
    {
        var pv1 = new HL7Segment("PV1", string.Empty);
        pv1.SetFieldValue(1, "1");
        
        // Patient Class
        if (encounter.Properties.TryGetValue("class", out var classObj))
        {
            var classElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(classObj));
            if (classElement.TryGetProperty("code", out var code))
            {
                var patientClass = code.GetString() switch
                {
                    "IMP" => "I",
                    "AMB" => "O",
                    "EMER" => "E",
                    _ => "O"
                };
                pv1.SetFieldValue(2, patientClass);
            }
        }

        pv1.SetFieldValue(19, encounter.Id);
        pv1.RebuildRawData();
        return pv1;
    }

    private HL7Segment CreateOBXSegment(FhirResource observation)
    {
        var obx = new HL7Segment("OBX", string.Empty);
        obx.SetFieldValue(1, "1");
        obx.SetFieldValue(2, "ST");
        
        // Observation ID
        if (observation.Properties.TryGetValue("code", out var codeObj))
        {
            var codeElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(codeObj));
            if (codeElement.TryGetProperty("coding", out var coding))
            {
                var codingArray = coding.EnumerateArray().FirstOrDefault();
                if (codingArray.TryGetProperty("code", out var code))
                {
                    obx.SetFieldValue(3, code.GetString());
                }
            }
        }

        // Observation Value
        if (observation.Properties.TryGetValue("valueString", out var valueObj))
        {
            obx.SetFieldValue(5, valueObj.ToString());
        }

        obx.RebuildRawData();
        return obx;
    }
}

public class CcdaFormatAdapter : IFormatAdapter<CcdaDocument>
{
    private readonly ILogger<CcdaFormatAdapter> _logger;

    public CcdaFormatAdapter(ILogger<CcdaFormatAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string AdapterName => "C-CDA Adapter";
    public string Description => "Converts HL7 v2 messages to C-CDA document format";
    public string SupportedVersion => "2.1";

    public bool CanConvertFrom(string data)
    {
        try
        {
            var doc = XDocument.Parse(data);
            return doc.Root?.Name.LocalName == "ClinicalDocument";
        }
        catch
        {
            return false;
        }
    }

    public bool CanConvertTo(HL7Message message)
    {
        return message.MessageType == HL7MessageType.ADT_A01 || 
               message.MessageType == HL7MessageType.ADT_A04 || 
               message.MessageType == HL7MessageType.ORU_R01;
    }

    public CcdaDocument ConvertFrom(HL7Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var document = new CcdaDocument
        {
            Id = message.Id,
            CreationTime = message.Timestamp,
            Title = $"Clinical Document - {message.MessageType}",
            PatientId = message.GetPatientId(),
            PatientName = message.GetPatientName()
        };

        // Add sections based on message content
        if (message.HasSegment("PID"))
        {
            document.Sections.Add(CreatePatientSection(message));
        }

        if (message.HasSegment("PV1"))
        {
            document.Sections.Add(CreateEncounterSection(message));
        }

        var observations = message.GetSegments("OBX");
        if (observations.Any())
        {
            document.Sections.Add(CreateResultsSection(observations));
        }

        _logger.LogInformation("Successfully converted HL7 message {MessageId} to C-CDA document", message.Id);
        return document;
    }

    public HL7Message ConvertTo(CcdaDocument data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var message = new HL7Message
        {
            Id = data.Id,
            Timestamp = data.CreationTime,
            Version = "2.5",
            MessageType = HL7MessageType.ADT_A01
        };

        // Create segments from C-CDA sections
        var mshSegment = new HL7Segment("MSH", string.Empty);
        mshSegment.SetFieldValue(1, "|");
        mshSegment.SetFieldValue(2, "^~\\&");
        mshSegment.SetFieldValue(3, "CCDA_ADAPTER");
        mshSegment.SetFieldValue(9, "ADT^A01");
        mshSegment.SetFieldValue(10, data.Id);
        mshSegment.SetFieldValue(11, "P");
        mshSegment.SetFieldValue(12, "2.5");
        mshSegment.RebuildRawData();
        message.AddSegment(mshSegment);

        // Create PID segment from patient section
        var patientSection = data.Sections.FirstOrDefault(s => s.Title.Contains("Patient"));
        if (patientSection != null)
        {
            var pidSegment = CreatePIDFromSection(patientSection, data);
            message.AddSegment(pidSegment);
        }

        message.RawMessage = string.Join("\r", message.Segments.Select(s => s.RawData));

        _logger.LogInformation("Successfully converted C-CDA document to HL7 message {MessageId}", message.Id);
        return message;
    }

    public ValidationResult ValidateData(CcdaDocument data)
    {
        var result = new ValidationResult { IsValid = true };

        if (data == null)
        {
            result.AddError("C-CDA document cannot be null");
            return result;
        }

        if (string.IsNullOrEmpty(data.Id))
        {
            result.AddError("C-CDA document must have an ID");
        }

        if (string.IsNullOrEmpty(data.Title))
        {
            result.AddWarning("C-CDA document should have a title");
        }

        if (string.IsNullOrEmpty(data.PatientId))
        {
            result.AddError("C-CDA document must have a patient ID");
        }

        result.AddMetadata("SectionCount", data.Sections.Count);
        result.AddMetadata("HasPatientSection", data.Sections.Any(s => s.Title.Contains("Patient")));

        return result;
    }

    private CcdaSection CreatePatientSection(HL7Message message)
    {
        return new CcdaSection
        {
            Title = "Patient Demographics",
            Code = "29762-2",
            Content = new Dictionary<string, object>
            {
                ["PatientId"] = message.GetPatientId(),
                ["PatientName"] = message.GetPatientName(),
                ["Gender"] = message.GetPatientGender(),
                ["BirthDate"] = message.GetPatientBirthDate()
            }
        };
    }

    private CcdaSection CreateEncounterSection(HL7Message message)
    {
        return new CcdaSection
        {
            Title = "Encounters",
            Code = "46240-8",
            Content = new Dictionary<string, object>
            {
                ["VisitNumber"] = message.GetVisitNumber(),
                ["PatientClass"] = message.GetPatientClass(),
                ["Location"] = message.GetAssignedPatientLocation(),
                ["AttendingDoctor"] = message.GetAttendingDoctor()
            }
        };
    }

    private CcdaSection CreateResultsSection(List<HL7Segment> observations)
    {
        var results = observations.Select(obs => new
        {
            ObservationId = obs.GetFieldValue(3),
            Value = obs.GetFieldValue(5),
            Units = obs.GetFieldValue(6),
            ReferenceRange = obs.GetFieldValue(7)
        }).ToList();

        return new CcdaSection
        {
            Title = "Results",
            Code = "30954-2",
            Content = new Dictionary<string, object>
            {
                ["Observations"] = results
            }
        };
    }

    private HL7Segment CreatePIDFromSection(CcdaSection section, CcdaDocument document)
    {
        var pid = new HL7Segment("PID", string.Empty);
        pid.SetFieldValue(1, "1");
        pid.SetFieldValue(3, document.PatientId);
        
        if (section.Content.TryGetValue("PatientName", out var name))
        {
            pid.SetFieldValue(5, name.ToString());
        }

        if (section.Content.TryGetValue("Gender", out var gender))
        {
            pid.SetFieldValue(8, gender.ToString());
        }

        if (section.Content.TryGetValue("BirthDate", out var birthDate))
        {
            if (DateTime.TryParse(birthDate.ToString(), out var date))
            {
                pid.SetFieldValue(7, date.ToString("yyyyMMdd"));
            }
        }

        pid.RebuildRawData();
        return pid;
    }
}

// Supporting classes for format adapters
public class FhirBundle
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<FhirResource> Resources { get; set; } = new();
}

public class FhirResource
{
    public string ResourceType { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class CcdaDocument
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public List<CcdaSection> Sections { get; set; } = new();
}

public class CcdaSection
{
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, object> Content { get; set; } = new();
}