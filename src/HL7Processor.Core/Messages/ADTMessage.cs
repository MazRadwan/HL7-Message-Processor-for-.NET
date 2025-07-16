using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;

namespace HL7Processor.Core.Messages;

public class ADTMessage : HL7Message
{
    public ADTMessage() : base()
    {
    }

    public ADTMessage(HL7Message baseMessage) : base()
    {
        Id = baseMessage.Id;
        MessageType = baseMessage.MessageType;
        Version = baseMessage.Version;
        RawMessage = baseMessage.RawMessage;
        Segments = baseMessage.Segments;
        SendingApplication = baseMessage.SendingApplication;
        SendingFacility = baseMessage.SendingFacility;
        ReceivingApplication = baseMessage.ReceivingApplication;
        ReceivingFacility = baseMessage.ReceivingFacility;
        MessageControlId = baseMessage.MessageControlId;
        ProcessingId = baseMessage.ProcessingId;
        Timestamp = baseMessage.Timestamp;
        IsValid = baseMessage.IsValid;
        ValidationErrors = baseMessage.ValidationErrors;
        Metadata = baseMessage.Metadata;
    }

    public string PatientId => this.GetPatientId();
    public string PatientName => this.GetPatientName();
    public DateTime? PatientBirthDate => this.GetPatientBirthDate();
    public string PatientGender => this.GetPatientGender();
    public string EventType => this.GetEventType();
    public DateTime? EventDateTime => this.GetEventDateTime();
    public string VisitNumber => this.GetVisitNumber();
    public string PatientClass => this.GetPatientClass();
    public string AssignedPatientLocation => this.GetAssignedPatientLocation();
    public string AttendingDoctor => this.GetAttendingDoctor();
    public List<string> Allergies => this.GetAllergies();

    public PatientInfo GetPatientInfo()
    {
        return new PatientInfo
        {
            PatientId = PatientId,
            Name = PatientName,
            BirthDate = PatientBirthDate,
            Gender = PatientGender,
            Allergies = Allergies
        };
    }

    public VisitInfo GetVisitInfo()
    {
        return new VisitInfo
        {
            VisitNumber = VisitNumber,
            PatientClass = PatientClass,
            AssignedLocation = AssignedPatientLocation,
            AttendingDoctor = AttendingDoctor,
            EventType = EventType,
            EventDateTime = EventDateTime
        };
    }

    public bool IsAdmission()
    {
        return MessageType == HL7MessageType.ADT_A01 || MessageType == HL7MessageType.ADT_A04;
    }

    public bool IsDischarge()
    {
        return MessageType == HL7MessageType.ADT_A03;
    }

    public bool IsTransfer()
    {
        return MessageType == HL7MessageType.ADT_A02;
    }

    public bool IsUpdate()
    {
        return MessageType == HL7MessageType.ADT_A08;
    }

    public bool IsCancellation()
    {
        return MessageType == HL7MessageType.ADT_A11 || 
               MessageType == HL7MessageType.ADT_A12 || 
               MessageType == HL7MessageType.ADT_A13;
    }

    public string GetPreviousPatientLocation()
    {
        var pv1Segment = GetSegment("PV1");
        return pv1Segment?.GetFieldValue(6) ?? string.Empty;
    }

    public string GetAdmissionType()
    {
        var pv1Segment = GetSegment("PV1");
        return pv1Segment?.GetFieldValue(4) ?? string.Empty;
    }

    public string GetHospitalService()
    {
        var pv1Segment = GetSegment("PV1");
        return pv1Segment?.GetFieldValue(10) ?? string.Empty;
    }

    public DateTime? GetAdmissionDateTime()
    {
        var pv1Segment = GetSegment("PV1");
        var admissionTimeField = pv1Segment?.GetFieldValue(44);
        
        if (string.IsNullOrEmpty(admissionTimeField))
            return null;

        return TryParseHL7DateTime(admissionTimeField);
    }

    public DateTime? GetDischargeDateTime()
    {
        var pv1Segment = GetSegment("PV1");
        var dischargeTimeField = pv1Segment?.GetFieldValue(45);
        
        if (string.IsNullOrEmpty(dischargeTimeField))
            return null;

        return TryParseHL7DateTime(dischargeTimeField);
    }

    public List<DiagnosisInfo> GetDiagnoses()
    {
        var diagnoses = new List<DiagnosisInfo>();
        var dg1Segments = GetSegments("DG1");

        foreach (var segment in dg1Segments)
        {
            var diagnosis = new DiagnosisInfo
            {
                DiagnosisCode = segment.GetFieldValue(3),
                DiagnosisDescription = segment.GetFieldValue(4),
                DiagnosisType = segment.GetFieldValue(6),
                DiagnosisDateTime = TryParseHL7DateTime(segment.GetFieldValue(5))
            };

            diagnoses.Add(diagnosis);
        }

        return diagnoses;
    }

    public List<InsuranceInfo> GetInsuranceInfo()
    {
        var insuranceList = new List<InsuranceInfo>();
        var in1Segments = GetSegments("IN1");

        foreach (var segment in in1Segments)
        {
            var insurance = new InsuranceInfo
            {
                InsuranceCompanyId = segment.GetFieldValue(3),
                InsuranceCompanyName = segment.GetFieldValue(4),
                PolicyNumber = segment.GetFieldValue(36),
                GroupNumber = segment.GetFieldValue(8)
            };

            insuranceList.Add(insurance);
        }

        return insuranceList;
    }

    private DateTime? TryParseHL7DateTime(string? hl7DateTime)
    {
        if (string.IsNullOrEmpty(hl7DateTime))
            return null;

        var cleanDateTime = hl7DateTime.Split('+')[0].Split('-')[0];
        
        if (cleanDateTime.Length < 8)
            return null;

        try
        {
            var year = int.Parse(cleanDateTime.Substring(0, 4));
            var month = int.Parse(cleanDateTime.Substring(4, 2));
            var day = int.Parse(cleanDateTime.Substring(6, 2));

            var hour = 0;
            var minute = 0;
            var second = 0;

            if (cleanDateTime.Length >= 10)
                hour = int.Parse(cleanDateTime.Substring(8, 2));

            if (cleanDateTime.Length >= 12)
                minute = int.Parse(cleanDateTime.Substring(10, 2));

            if (cleanDateTime.Length >= 14)
                second = int.Parse(cleanDateTime.Substring(12, 2));

            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            return null;
        }
    }
}

public class PatientInfo
{
    public string PatientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public List<string> Allergies { get; set; } = new();
}

public class VisitInfo
{
    public string VisitNumber { get; set; } = string.Empty;
    public string PatientClass { get; set; } = string.Empty;
    public string AssignedLocation { get; set; } = string.Empty;
    public string AttendingDoctor { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime? EventDateTime { get; set; }
}

public class DiagnosisInfo
{
    public string DiagnosisCode { get; set; } = string.Empty;
    public string DiagnosisDescription { get; set; } = string.Empty;
    public string DiagnosisType { get; set; } = string.Empty;
    public DateTime? DiagnosisDateTime { get; set; }
}

public class InsuranceInfo
{
    public string InsuranceCompanyId { get; set; } = string.Empty;
    public string InsuranceCompanyName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string GroupNumber { get; set; } = string.Empty;
}