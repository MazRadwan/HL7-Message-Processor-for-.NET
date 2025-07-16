using HL7Processor.Core.Models;
using HL7Processor.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace HL7Processor.Core.Transformation.Converters;

public class XmlConverter
{
    private readonly ILogger<XmlConverter> _logger;

    public XmlConverter(ILogger<XmlConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ConvertHL7ToXml(HL7Message message, bool prettyPrint = true, bool includeMetadata = true)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            var document = ConvertHL7ToXmlDocument(message, includeMetadata);
            
            var settings = new XmlWriterSettings
            {
                Indent = prettyPrint,
                IndentChars = "  ",
                NewLineChars = "\n",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            
            document.WriteTo(xmlWriter);
            xmlWriter.Flush();
            
            var xml = stringWriter.ToString();
            
            _logger.LogDebug("Successfully converted HL7 message {MessageId} to XML ({Length} characters)", 
                message.Id, xml.Length);
            
            return xml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HL7 message {MessageId} to XML", message.Id);
            throw;
        }
    }

    public HL7Message ConvertXmlToHL7(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("XML cannot be null or empty", nameof(xml));

        try
        {
            var document = XDocument.Parse(xml);
            var message = ConvertXmlDocumentToHL7(document);
            
            _logger.LogDebug("Successfully converted XML to HL7 message {MessageId}", message.Id);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert XML to HL7 message");
            throw;
        }
    }

    public string ConvertHL7ToClinicalDocument(HL7Message message, string templateId = "2.16.840.1.113883.10.20.22.1.1")
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            var document = CreateClinicalDocument(message, templateId);
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            
            document.WriteTo(xmlWriter);
            xmlWriter.Flush();
            
            var xml = stringWriter.ToString();
            
            _logger.LogDebug("Successfully converted HL7 message {MessageId} to Clinical Document ({Length} characters)", 
                message.Id, xml.Length);
            
            return xml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HL7 message {MessageId} to Clinical Document", message.Id);
            throw;
        }
    }

    private XDocument ConvertHL7ToXmlDocument(HL7Message message, bool includeMetadata)
    {
        var root = new XElement("HL7Message",
            new XAttribute("messageId", message.Id),
            new XAttribute("messageType", message.MessageType.ToString()),
            new XAttribute("timestamp", message.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
            new XAttribute("version", message.Version)
        );

        if (includeMetadata)
        {
            root.Add(
                new XAttribute("sendingApplication", message.SendingApplication ?? string.Empty),
                new XAttribute("sendingFacility", message.SendingFacility ?? string.Empty),
                new XAttribute("receivingApplication", message.ReceivingApplication ?? string.Empty),
                new XAttribute("receivingFacility", message.ReceivingFacility ?? string.Empty),
                new XAttribute("messageControlId", message.MessageControlId ?? string.Empty),
                new XAttribute("processingId", message.ProcessingId ?? string.Empty)
            );
        }

        var segmentsElement = new XElement("Segments");
        
        foreach (var segment in message.Segments)
        {
            var segmentElement = ConvertSegmentToXmlElement(segment);
            segmentsElement.Add(segmentElement);
        }

        root.Add(segmentsElement);
        
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            root
        );
    }

    private XElement ConvertSegmentToXmlElement(HL7Segment segment)
    {
        var segmentElement = new XElement("Segment",
            new XAttribute("type", segment.Type)
        );

        var delimiters = new HL7Delimiters(); // Use default delimiters for XML conversion

        for (int fieldIndex = 1; fieldIndex < segment.Fields.Count; fieldIndex++)
        {
            var fieldValue = segment.GetFieldValue(fieldIndex);
            if (!string.IsNullOrEmpty(fieldValue))
            {
                var fieldElement = ConvertFieldToXmlElement(fieldIndex, fieldValue, delimiters);
                segmentElement.Add(fieldElement);
            }
        }

        return segmentElement;
    }

    private XElement ConvertFieldToXmlElement(int fieldIndex, string fieldValue, HL7Delimiters delimiters)
    {
        var fieldElement = new XElement($"Field{fieldIndex}");

        // Check if field has repetitions
        var repetitions = HL7ParsingUtils.SplitField(fieldValue, delimiters);
        if (repetitions.Length > 1)
        {
            for (int repIndex = 0; repIndex < repetitions.Length; repIndex++)
            {
                var repetitionElement = ConvertRepetitionToXmlElement(repIndex + 1, repetitions[repIndex], delimiters);
                fieldElement.Add(repetitionElement);
            }
        }
        else
        {
            var components = HL7ParsingUtils.SplitComponent(fieldValue, delimiters);
            if (components.Length > 1)
            {
                for (int compIndex = 0; compIndex < components.Length; compIndex++)
                {
                    var componentElement = ConvertComponentToXmlElement(compIndex + 1, components[compIndex], delimiters);
                    fieldElement.Add(componentElement);
                }
            }
            else
            {
                fieldElement.Value = fieldValue;
            }
        }

        return fieldElement;
    }

    private XElement ConvertRepetitionToXmlElement(int repetitionIndex, string repetitionValue, HL7Delimiters delimiters)
    {
        var repetitionElement = new XElement($"Repetition{repetitionIndex}");

        var components = HL7ParsingUtils.SplitComponent(repetitionValue, delimiters);
        if (components.Length > 1)
        {
            for (int compIndex = 0; compIndex < components.Length; compIndex++)
            {
                var componentElement = ConvertComponentToXmlElement(compIndex + 1, components[compIndex], delimiters);
                repetitionElement.Add(componentElement);
            }
        }
        else
        {
            repetitionElement.Value = repetitionValue;
        }

        return repetitionElement;
    }

    private XElement ConvertComponentToXmlElement(int componentIndex, string componentValue, HL7Delimiters delimiters)
    {
        var componentElement = new XElement($"Component{componentIndex}");

        var subComponents = HL7ParsingUtils.SplitSubComponent(componentValue, delimiters);
        if (subComponents.Length > 1)
        {
            for (int subIndex = 0; subIndex < subComponents.Length; subIndex++)
            {
                var subComponentElement = new XElement($"SubComponent{subIndex + 1}")
                {
                    Value = subComponents[subIndex]
                };
                componentElement.Add(subComponentElement);
            }
        }
        else
        {
            componentElement.Value = componentValue;
        }

        return componentElement;
    }

    private HL7Message ConvertXmlDocumentToHL7(XDocument document)
    {
        var root = document.Root;
        if (root == null || root.Name != "HL7Message")
            throw new XmlException("Invalid XML format: root element must be 'HL7Message'");

        var message = new HL7Message
        {
            Id = root.Attribute("messageId")?.Value ?? string.Empty,
            Version = root.Attribute("version")?.Value ?? "2.5"
        };

        // Parse message type
        var messageTypeStr = root.Attribute("messageType")?.Value;
        if (!string.IsNullOrEmpty(messageTypeStr) && Enum.TryParse<HL7MessageType>(messageTypeStr, out var messageType))
        {
            message.MessageType = messageType;
        }

        // Parse timestamp
        var timestampStr = root.Attribute("timestamp")?.Value;
        if (!string.IsNullOrEmpty(timestampStr) && DateTime.TryParse(timestampStr, out var timestamp))
        {
            message.Timestamp = timestamp;
        }

        // Parse optional attributes
        message.SendingApplication = root.Attribute("sendingApplication")?.Value;
        message.SendingFacility = root.Attribute("sendingFacility")?.Value;
        message.ReceivingApplication = root.Attribute("receivingApplication")?.Value;
        message.ReceivingFacility = root.Attribute("receivingFacility")?.Value;
        message.MessageControlId = root.Attribute("messageControlId")?.Value;
        message.ProcessingId = root.Attribute("processingId")?.Value;

        // Parse segments
        var segmentsElement = root.Element("Segments");
        if (segmentsElement != null)
        {
            foreach (var segmentElement in segmentsElement.Elements("Segment"))
            {
                var segment = ConvertXmlElementToSegment(segmentElement);
                if (segment != null)
                {
                    message.AddSegment(segment);
                }
            }
        }

        // Rebuild raw message
        message.RawMessage = string.Join("\r", message.Segments.Select(s => s.RawData));

        return message;
    }

    private HL7Segment? ConvertXmlElementToSegment(XElement segmentElement)
    {
        var segmentType = segmentElement.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(segmentType))
            return null;

        var segment = new HL7Segment(segmentType, string.Empty);
        var delimiters = new HL7Delimiters();

        // Extract fields
        var fieldElements = segmentElement.Elements().Where(e => e.Name.LocalName.StartsWith("Field")).ToList();
        
        foreach (var fieldElement in fieldElements)
        {
            var fieldNumberStr = fieldElement.Name.LocalName.Substring(5); // Remove "Field" prefix
            if (int.TryParse(fieldNumberStr, out var fieldNumber))
            {
                var fieldValue = ConvertXmlElementToFieldValue(fieldElement, delimiters);
                segment.SetFieldValue(fieldNumber, fieldValue);
            }
        }

        segment.RebuildRawData();
        return segment;
    }

    private string ConvertXmlElementToFieldValue(XElement fieldElement, HL7Delimiters delimiters)
    {
        // Check if field has repetitions
        var repetitionElements = fieldElement.Elements().Where(e => e.Name.LocalName.StartsWith("Repetition")).ToList();
        if (repetitionElements.Count > 0)
        {
            var repetitionValues = new List<string>();
            foreach (var repetitionElement in repetitionElements.OrderBy(e => e.Name.LocalName))
            {
                var repetitionValue = ConvertXmlElementToRepetitionValue(repetitionElement, delimiters);
                repetitionValues.Add(repetitionValue);
            }
            return HL7ParsingUtils.JoinRepetitions(repetitionValues.ToArray(), delimiters);
        }

        // Check if field has components
        var componentElements = fieldElement.Elements().Where(e => e.Name.LocalName.StartsWith("Component")).ToList();
        if (componentElements.Count > 0)
        {
            var componentValues = new List<string>();
            foreach (var componentElement in componentElements.OrderBy(e => e.Name.LocalName))
            {
                var componentValue = ConvertXmlElementToComponentValue(componentElement, delimiters);
                componentValues.Add(componentValue);
            }
            return HL7ParsingUtils.JoinComponents(componentValues.ToArray(), delimiters);
        }

        // Simple field value
        return fieldElement.Value;
    }

    private string ConvertXmlElementToRepetitionValue(XElement repetitionElement, HL7Delimiters delimiters)
    {
        var componentElements = repetitionElement.Elements().Where(e => e.Name.LocalName.StartsWith("Component")).ToList();
        if (componentElements.Count > 0)
        {
            var componentValues = new List<string>();
            foreach (var componentElement in componentElements.OrderBy(e => e.Name.LocalName))
            {
                var componentValue = ConvertXmlElementToComponentValue(componentElement, delimiters);
                componentValues.Add(componentValue);
            }
            return HL7ParsingUtils.JoinComponents(componentValues.ToArray(), delimiters);
        }

        return repetitionElement.Value;
    }

    private string ConvertXmlElementToComponentValue(XElement componentElement, HL7Delimiters delimiters)
    {
        var subComponentElements = componentElement.Elements().Where(e => e.Name.LocalName.StartsWith("SubComponent")).ToList();
        if (subComponentElements.Count > 0)
        {
            var subComponentValues = new List<string>();
            foreach (var subComponentElement in subComponentElements.OrderBy(e => e.Name.LocalName))
            {
                subComponentValues.Add(subComponentElement.Value);
            }
            return HL7ParsingUtils.JoinSubComponents(subComponentValues.ToArray(), delimiters);
        }

        return componentElement.Value;
    }

    private XDocument CreateClinicalDocument(HL7Message message, string templateId)
    {
        var ns = XNamespace.Get("urn:hl7-org:v3");
        
        var root = new XElement(ns + "ClinicalDocument",
            new XAttribute("xmlns", ns.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute(XNamespace.Xmlns + "voc", "urn:hl7-org:v3/voc"),
            new XAttribute("classCode", "DOCCLIN"),
            new XAttribute("moodCode", "EVN")
        );

        // Template ID
        root.Add(new XElement(ns + "templateId",
            new XAttribute("root", templateId)
        ));

        // Document ID
        root.Add(new XElement(ns + "id",
            new XAttribute("root", message.Id)
        ));

        // Code
        root.Add(new XElement(ns + "code",
            new XAttribute("code", "34133-9"),
            new XAttribute("displayName", "Summarization of Episode Note"),
            new XAttribute("codeSystem", "2.16.840.1.113883.6.1"),
            new XAttribute("codeSystemName", "LOINC")
        ));

        // Title
        root.Add(new XElement(ns + "title", $"Clinical Document - {message.MessageType}"));

        // Effective Time
        root.Add(new XElement(ns + "effectiveTime",
            new XAttribute("value", message.Timestamp.ToString("yyyyMMddHHmmss"))
        ));

        // Confidentiality Code
        root.Add(new XElement(ns + "confidentialityCode",
            new XAttribute("code", "N"),
            new XAttribute("codeSystem", "2.16.840.1.113883.5.25")
        ));

        // Language Code
        root.Add(new XElement(ns + "languageCode",
            new XAttribute("code", "en-US")
        ));

        // Add patient information if available
        var pidSegment = message.GetSegment("PID");
        if (pidSegment != null)
        {
            var recordTarget = CreateRecordTarget(ns, pidSegment);
            root.Add(recordTarget);
        }

        // Add author information
        var author = CreateAuthor(ns, message);
        root.Add(author);

        // Add custodian
        var custodian = CreateCustodian(ns, message);
        root.Add(custodian);

        // Add component with structured body
        var component = CreateComponent(ns, message);
        root.Add(component);

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            root
        );
    }

    private XElement CreateRecordTarget(XNamespace ns, HL7Segment pidSegment)
    {
        var recordTarget = new XElement(ns + "recordTarget");
        var patientRole = new XElement(ns + "patientRole");

        // Patient ID
        var patientId = pidSegment.GetFieldValue(3);
        if (!string.IsNullOrEmpty(patientId))
        {
            patientRole.Add(new XElement(ns + "id",
                new XAttribute("root", "2.16.840.1.113883.19.5"),
                new XAttribute("extension", patientId)
            ));
        }

        // Patient
        var patient = new XElement(ns + "patient");

        // Name
        var patientName = pidSegment.GetFieldValue(5);
        if (!string.IsNullOrEmpty(patientName))
        {
            var nameParts = patientName.Split('^');
            var name = new XElement(ns + "name");
            
            if (nameParts.Length > 1)
            {
                name.Add(new XElement(ns + "given", nameParts[1])); // First name
            }
            if (nameParts.Length > 0)
            {
                name.Add(new XElement(ns + "family", nameParts[0])); // Last name
            }
            
            patient.Add(name);
        }

        // Gender
        var gender = pidSegment.GetFieldValue(8);
        if (!string.IsNullOrEmpty(gender))
        {
            var genderCode = gender.ToUpperInvariant() switch
            {
                "M" => "M",
                "F" => "F",
                _ => "UN"
            };
            
            patient.Add(new XElement(ns + "administrativeGenderCode",
                new XAttribute("code", genderCode),
                new XAttribute("codeSystem", "2.16.840.1.113883.5.1")
            ));
        }

        // Birth date
        var birthDate = pidSegment.GetFieldValue(7);
        if (!string.IsNullOrEmpty(birthDate))
        {
            patient.Add(new XElement(ns + "birthTime",
                new XAttribute("value", birthDate)
            ));
        }

        patientRole.Add(patient);
        recordTarget.Add(patientRole);

        return recordTarget;
    }

    private XElement CreateAuthor(XNamespace ns, HL7Message message)
    {
        var author = new XElement(ns + "author");
        
        author.Add(new XElement(ns + "time",
            new XAttribute("value", message.Timestamp.ToString("yyyyMMddHHmmss"))
        ));

        var assignedAuthor = new XElement(ns + "assignedAuthor");
        
        assignedAuthor.Add(new XElement(ns + "id",
            new XAttribute("root", "2.16.840.1.113883.19.5"),
            new XAttribute("extension", "999999999")
        ));

        var assignedPerson = new XElement(ns + "assignedPerson");
        assignedPerson.Add(new XElement(ns + "name",
            new XElement(ns + "given", "System"),
            new XElement(ns + "family", "Generated")
        ));

        assignedAuthor.Add(assignedPerson);
        author.Add(assignedAuthor);

        return author;
    }

    private XElement CreateCustodian(XNamespace ns, HL7Message message)
    {
        var custodian = new XElement(ns + "custodian");
        var assignedCustodian = new XElement(ns + "assignedCustodian");
        var representedCustodianOrganization = new XElement(ns + "representedCustodianOrganization");

        representedCustodianOrganization.Add(new XElement(ns + "id",
            new XAttribute("root", "2.16.840.1.113883.19.5")
        ));

        representedCustodianOrganization.Add(new XElement(ns + "name", 
            message.SendingFacility ?? "Healthcare Facility"));

        assignedCustodian.Add(representedCustodianOrganization);
        custodian.Add(assignedCustodian);

        return custodian;
    }

    private XElement CreateComponent(XNamespace ns, HL7Message message)
    {
        var component = new XElement(ns + "component");
        var structuredBody = new XElement(ns + "structuredBody");

        // Create sections for each segment type
        var segmentGroups = message.Segments.GroupBy(s => s.Type);
        
        foreach (var group in segmentGroups)
        {
            if (group.Key != "MSH") // Skip MSH as it's header info
            {
                var section = CreateSection(ns, group.Key, group.ToList());
                structuredBody.Add(section);
            }
        }

        component.Add(structuredBody);
        return component;
    }

    private XElement CreateSection(XNamespace ns, string segmentType, List<HL7Segment> segments)
    {
        var component = new XElement(ns + "component");
        var section = new XElement(ns + "section");

        // Template ID based on segment type
        var templateId = segmentType switch
        {
            "PID" => "2.16.840.1.113883.10.20.22.2.6.1", // Patient Demographics
            "PV1" => "2.16.840.1.113883.10.20.22.2.22.1", // Encounters
            "OBX" => "2.16.840.1.113883.10.20.22.2.3.1",  // Results
            _ => "2.16.840.1.113883.10.20.22.2.10"        // General
        };

        section.Add(new XElement(ns + "templateId",
            new XAttribute("root", templateId)
        ));

        // Section code
        var sectionCode = segmentType switch
        {
            "PID" => ("29762-2", "Social History"),
            "PV1" => ("46240-8", "History of Encounters"),
            "OBX" => ("30954-2", "Relevant diagnostic tests and/or laboratory data"),
            _ => ("10164-2", "History of Present Illness")
        };

        section.Add(new XElement(ns + "code",
            new XAttribute("code", sectionCode.Item1),
            new XAttribute("displayName", sectionCode.Item2),
            new XAttribute("codeSystem", "2.16.840.1.113883.6.1")
        ));

        section.Add(new XElement(ns + "title", $"{segmentType} Information"));

        // Text content
        var text = new XElement(ns + "text");
        var table = new XElement("table",
            new XAttribute("border", "1"),
            new XAttribute("width", "100%")
        );

        // Add table headers
        var headerRow = new XElement("thead",
            new XElement("tr")
        );
        
        if (segments.Count > 0)
        {
            for (int i = 1; i < segments[0].Fields.Count; i++)
            {
                headerRow.Element("tr")?.Add(new XElement("th", $"Field {i}"));
            }
        }
        table.Add(headerRow);

        // Add data rows
        var tbody = new XElement("tbody");
        foreach (var segment in segments)
        {
            var row = new XElement("tr");
            for (int i = 1; i < segment.Fields.Count; i++)
            {
                var fieldValue = segment.GetFieldValue(i);
                row.Add(new XElement("td", fieldValue ?? string.Empty));
            }
            tbody.Add(row);
        }
        table.Add(tbody);

        text.Add(table);
        section.Add(text);

        component.Add(section);
        return component;
    }
}