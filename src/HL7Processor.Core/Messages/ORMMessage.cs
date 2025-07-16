using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;

namespace HL7Processor.Core.Messages;

public class ORMMessage : HL7Message
{
    public ORMMessage() : base()
    {
    }

    public ORMMessage(HL7Message baseMessage) : base()
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
    public string VisitNumber => this.GetVisitNumber();

    public List<OrderInfo> GetOrders()
    {
        var orders = new List<OrderInfo>();
        var orcSegments = GetSegments("ORC");

        foreach (var orcSegment in orcSegments)
        {
            var order = new OrderInfo
            {
                OrderControl = orcSegment.GetFieldValue(1),
                PlacerOrderNumber = orcSegment.GetFieldValue(2),
                FillerOrderNumber = orcSegment.GetFieldValue(3),
                OrderStatus = orcSegment.GetFieldValue(5),
                OrderingProvider = orcSegment.GetFieldValue(12),
                OrderDateTime = TryParseHL7DateTime(orcSegment.GetFieldValue(9))
            };

            // Find corresponding OBR segment
            var obrSegment = FindCorrespondingOBRSegment(orcSegment);
            if (obrSegment != null)
            {
                order.UniversalServiceId = obrSegment.GetFieldValue(4);
                order.Priority = obrSegment.GetFieldValue(5);
                order.RequestedDateTime = TryParseHL7DateTime(obrSegment.GetFieldValue(6));
                order.ObservationDateTime = TryParseHL7DateTime(obrSegment.GetFieldValue(7));
                order.CollectorId = obrSegment.GetFieldValue(10);
                order.SpecimenActionCode = obrSegment.GetFieldValue(11);
                order.RelevantClinicalInfo = obrSegment.GetFieldValue(13);
                order.SpecimenReceivedDateTime = TryParseHL7DateTime(obrSegment.GetFieldValue(14));
                order.OrderingFacilityName = obrSegment.GetFieldValue(21);
                order.OrderingFacilityAddress = obrSegment.GetFieldValue(22);
                order.OrderingFacilityPhoneNumber = obrSegment.GetFieldValue(23);
                order.OrderingProviderAddress = obrSegment.GetFieldValue(24);
            }

            orders.Add(order);
        }

        return orders;
    }

    public List<ObservationInfo> GetObservations()
    {
        var observations = new List<ObservationInfo>();
        var obxSegments = GetSegments("OBX");

        foreach (var obxSegment in obxSegments)
        {
            var observation = new ObservationInfo
            {
                SetId = obxSegment.GetFieldValue(1),
                ValueType = obxSegment.GetFieldValue(2),
                ObservationId = obxSegment.GetFieldValue(3),
                ObservationSubId = obxSegment.GetFieldValue(4),
                ObservationValue = obxSegment.GetFieldValue(5),
                Units = obxSegment.GetFieldValue(6),
                ReferenceRange = obxSegment.GetFieldValue(7),
                AbnormalFlags = obxSegment.GetFieldValue(8),
                Probability = obxSegment.GetFieldValue(9),
                NatureOfAbnormalTest = obxSegment.GetFieldValue(10),
                ObservationResultStatus = obxSegment.GetFieldValue(11),
                DateOfLastObservation = TryParseHL7DateTime(obxSegment.GetFieldValue(12)),
                UserDefinedAccessChecks = obxSegment.GetFieldValue(13),
                ObservationDateTime = TryParseHL7DateTime(obxSegment.GetFieldValue(14)),
                ProducersId = obxSegment.GetFieldValue(15),
                ResponsibleObserver = obxSegment.GetFieldValue(16)
            };

            observations.Add(observation);
        }

        return observations;
    }

    public List<NoteInfo> GetNotes()
    {
        var notes = new List<NoteInfo>();
        var nteSegments = GetSegments("NTE");

        foreach (var nteSegment in nteSegments)
        {
            var note = new NoteInfo
            {
                SetId = nteSegment.GetFieldValue(1),
                SourceOfComment = nteSegment.GetFieldValue(2),
                Comment = nteSegment.GetFieldValue(3),
                CommentType = nteSegment.GetFieldValue(4)
            };

            notes.Add(note);
        }

        return notes;
    }

    public bool IsNewOrder()
    {
        var orders = GetOrders();
        return orders.Any(o => o.OrderControl?.ToUpperInvariant() == "NW");
    }

    public bool IsOrderCancellation()
    {
        var orders = GetOrders();
        return orders.Any(o => o.OrderControl?.ToUpperInvariant() == "CA");
    }

    public bool IsOrderUpdate()
    {
        var orders = GetOrders();
        return orders.Any(o => o.OrderControl?.ToUpperInvariant() == "XO");
    }

    public string GetOrderingFacility()
    {
        var orders = GetOrders();
        return orders.FirstOrDefault()?.OrderingFacilityName ?? string.Empty;
    }

    public string GetOrderingProvider()
    {
        var orders = GetOrders();
        return orders.FirstOrDefault()?.OrderingProvider ?? string.Empty;
    }

    public DateTime? GetLatestOrderDateTime()
    {
        var orders = GetOrders();
        return orders.Where(o => o.OrderDateTime.HasValue)
                    .OrderByDescending(o => o.OrderDateTime)
                    .FirstOrDefault()?.OrderDateTime;
    }

    private HL7Segment? FindCorrespondingOBRSegment(HL7Segment orcSegment)
    {
        var obrSegments = GetSegments("OBR");
        var placerOrderNumber = orcSegment.GetFieldValue(2);
        
        if (string.IsNullOrEmpty(placerOrderNumber))
            return obrSegments.FirstOrDefault();

        return obrSegments.FirstOrDefault(obr => 
            obr.GetFieldValue(2) == placerOrderNumber || 
            obr.GetFieldValue(3) == orcSegment.GetFieldValue(3));
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

public class OrderInfo
{
    public string OrderControl { get; set; } = string.Empty;
    public string PlacerOrderNumber { get; set; } = string.Empty;
    public string FillerOrderNumber { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string OrderingProvider { get; set; } = string.Empty;
    public DateTime? OrderDateTime { get; set; }
    public string UniversalServiceId { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? RequestedDateTime { get; set; }
    public DateTime? ObservationDateTime { get; set; }
    public string CollectorId { get; set; } = string.Empty;
    public string SpecimenActionCode { get; set; } = string.Empty;
    public string RelevantClinicalInfo { get; set; } = string.Empty;
    public DateTime? SpecimenReceivedDateTime { get; set; }
    public string OrderingFacilityName { get; set; } = string.Empty;
    public string OrderingFacilityAddress { get; set; } = string.Empty;
    public string OrderingFacilityPhoneNumber { get; set; } = string.Empty;
    public string OrderingProviderAddress { get; set; } = string.Empty;
}

public class ObservationInfo
{
    public string SetId { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string ObservationId { get; set; } = string.Empty;
    public string ObservationSubId { get; set; } = string.Empty;
    public string ObservationValue { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public string ReferenceRange { get; set; } = string.Empty;
    public string AbnormalFlags { get; set; } = string.Empty;
    public string Probability { get; set; } = string.Empty;
    public string NatureOfAbnormalTest { get; set; } = string.Empty;
    public string ObservationResultStatus { get; set; } = string.Empty;
    public DateTime? DateOfLastObservation { get; set; }
    public string UserDefinedAccessChecks { get; set; } = string.Empty;
    public DateTime? ObservationDateTime { get; set; }
    public string ProducersId { get; set; } = string.Empty;
    public string ResponsibleObserver { get; set; } = string.Empty;
}

public class NoteInfo
{
    public string SetId { get; set; } = string.Empty;
    public string SourceOfComment { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string CommentType { get; set; } = string.Empty;
}