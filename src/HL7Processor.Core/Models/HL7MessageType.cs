namespace HL7Processor.Core.Models;

public enum HL7MessageType
{
    Unknown = 0,
    
    // Admission, Discharge, Transfer
    ADT_A01 = 1,    // Admit/visit notification
    ADT_A02 = 2,    // Transfer a patient
    ADT_A03 = 3,    // Discharge/end visit
    ADT_A04 = 4,    // Register a patient
    ADT_A05 = 5,    // Pre-admit a patient
    ADT_A06 = 6,    // Change an outpatient to an inpatient
    ADT_A07 = 7,    // Change an inpatient to an outpatient
    ADT_A08 = 8,    // Update patient information
    ADT_A09 = 9,    // Patient departing - tracking
    ADT_A10 = 10,   // Patient arriving - tracking
    ADT_A11 = 11,   // Cancel admit/visit notification
    ADT_A12 = 12,   // Cancel transfer
    ADT_A13 = 13,   // Cancel discharge/end visit
    
    // Order Entry
    ORM_O01 = 20,   // Order message
    ORU_R01 = 21,   // Observation result
    ORR_O02 = 22,   // Order response
    ORC_O01 = 23,   // Common order
    
    // Query
    QRY_A19 = 30,   // Patient query
    QRY_PC4 = 31,   // Patient care query
    QRY_Q01 = 32,   // Query general
    QRY_Q02 = 33,   // Query by parameter
    
    // Master Files
    MFN_M01 = 40,   // Master file notification
    MFN_M02 = 41,   // Master file - staff practitioner
    MFN_M03 = 42,   // Master file - test/observation
    
    // Application Control
    ACK = 50,       // General acknowledgment
    NAK = 51,       // General negative acknowledgment
    
    // Financial Management
    DFT_P03 = 60,   // Post detail financial transaction
    BAR_P01 = 61,   // Add patient accounts
    BAR_P02 = 62,   // Purge patient accounts
    
    // Scheduling
    SIU_S12 = 70,   // Notification of new appointment booking
    SIU_S13 = 71,   // Notification of appointment rescheduling
    SIU_S14 = 72,   // Notification of appointment modification
    SIU_S15 = 73,   // Notification of appointment cancellation
    SIU_S16 = 74,   // Notification of appointment discontinuation
    SIU_S17 = 75,   // Notification of appointment deletion
    
    // Medical Records/Information Management
    MDM_T01 = 80,   // Original document notification
    MDM_T02 = 81,   // Original document notification and content
    MDM_T03 = 82,   // Document status change notification
    MDM_T04 = 83,   // Document status change notification and content
    
    // Pharmacy/Treatment Administration
    RAS_O17 = 90,   // Pharmacy/treatment administration
    RDE_O11 = 91,   // Pharmacy/treatment encoded order
    RDS_O13 = 92,   // Pharmacy/treatment dispense
    RGV_O15 = 93,   // Pharmacy/treatment give
    
    // Laboratory Automation
    LAB_L01 = 100,  // Laboratory order
    LAB_L02 = 101,  // Laboratory result
    LAB_L03 = 102,  // Laboratory status
}

public static class HL7MessageTypeExtensions
{
    public static string GetMessageTypeCode(this HL7MessageType messageType)
    {
        return messageType switch
        {
            HL7MessageType.ADT_A01 => "ADT^A01",
            HL7MessageType.ADT_A02 => "ADT^A02",
            HL7MessageType.ADT_A03 => "ADT^A03",
            HL7MessageType.ADT_A04 => "ADT^A04",
            HL7MessageType.ADT_A05 => "ADT^A05",
            HL7MessageType.ADT_A06 => "ADT^A06",
            HL7MessageType.ADT_A07 => "ADT^A07",
            HL7MessageType.ADT_A08 => "ADT^A08",
            HL7MessageType.ADT_A09 => "ADT^A09",
            HL7MessageType.ADT_A10 => "ADT^A10",
            HL7MessageType.ADT_A11 => "ADT^A11",
            HL7MessageType.ADT_A12 => "ADT^A12",
            HL7MessageType.ADT_A13 => "ADT^A13",
            HL7MessageType.ORM_O01 => "ORM^O01",
            HL7MessageType.ORU_R01 => "ORU^R01",
            HL7MessageType.ORR_O02 => "ORR^O02",
            HL7MessageType.ORC_O01 => "ORC^O01",
            HL7MessageType.QRY_A19 => "QRY^A19",
            HL7MessageType.QRY_PC4 => "QRY^PC4",
            HL7MessageType.QRY_Q01 => "QRY^Q01",
            HL7MessageType.QRY_Q02 => "QRY^Q02",
            HL7MessageType.MFN_M01 => "MFN^M01",
            HL7MessageType.MFN_M02 => "MFN^M02",
            HL7MessageType.MFN_M03 => "MFN^M03",
            HL7MessageType.ACK => "ACK",
            HL7MessageType.NAK => "NAK",
            HL7MessageType.DFT_P03 => "DFT^P03",
            HL7MessageType.BAR_P01 => "BAR^P01",
            HL7MessageType.BAR_P02 => "BAR^P02",
            HL7MessageType.SIU_S12 => "SIU^S12",
            HL7MessageType.SIU_S13 => "SIU^S13",
            HL7MessageType.SIU_S14 => "SIU^S14",
            HL7MessageType.SIU_S15 => "SIU^S15",
            HL7MessageType.SIU_S16 => "SIU^S16",
            HL7MessageType.SIU_S17 => "SIU^S17",
            HL7MessageType.MDM_T01 => "MDM^T01",
            HL7MessageType.MDM_T02 => "MDM^T02",
            HL7MessageType.MDM_T03 => "MDM^T03",
            HL7MessageType.MDM_T04 => "MDM^T04",
            HL7MessageType.RAS_O17 => "RAS^O17",
            HL7MessageType.RDE_O11 => "RDE^O11",
            HL7MessageType.RDS_O13 => "RDS^O13",
            HL7MessageType.RGV_O15 => "RGV^O15",
            HL7MessageType.LAB_L01 => "LAB^L01",
            HL7MessageType.LAB_L02 => "LAB^L02",
            HL7MessageType.LAB_L03 => "LAB^L03",
            _ => "UNKNOWN"
        };
    }

    public static HL7MessageType FromMessageTypeCode(string messageTypeCode)
    {
        return messageTypeCode?.ToUpperInvariant() switch
        {
            "ADT^A01" => HL7MessageType.ADT_A01,
            "ADT^A02" => HL7MessageType.ADT_A02,
            "ADT^A03" => HL7MessageType.ADT_A03,
            "ADT^A04" => HL7MessageType.ADT_A04,
            "ADT^A05" => HL7MessageType.ADT_A05,
            "ADT^A06" => HL7MessageType.ADT_A06,
            "ADT^A07" => HL7MessageType.ADT_A07,
            "ADT^A08" => HL7MessageType.ADT_A08,
            "ADT^A09" => HL7MessageType.ADT_A09,
            "ADT^A10" => HL7MessageType.ADT_A10,
            "ADT^A11" => HL7MessageType.ADT_A11,
            "ADT^A12" => HL7MessageType.ADT_A12,
            "ADT^A13" => HL7MessageType.ADT_A13,
            "ORM^O01" => HL7MessageType.ORM_O01,
            "ORU^R01" => HL7MessageType.ORU_R01,
            "ORR^O02" => HL7MessageType.ORR_O02,
            "ORC^O01" => HL7MessageType.ORC_O01,
            "QRY^A19" => HL7MessageType.QRY_A19,
            "QRY^PC4" => HL7MessageType.QRY_PC4,
            "QRY^Q01" => HL7MessageType.QRY_Q01,
            "QRY^Q02" => HL7MessageType.QRY_Q02,
            "MFN^M01" => HL7MessageType.MFN_M01,
            "MFN^M02" => HL7MessageType.MFN_M02,
            "MFN^M03" => HL7MessageType.MFN_M03,
            "ACK" => HL7MessageType.ACK,
            "NAK" => HL7MessageType.NAK,
            "DFT^P03" => HL7MessageType.DFT_P03,
            "BAR^P01" => HL7MessageType.BAR_P01,
            "BAR^P02" => HL7MessageType.BAR_P02,
            "SIU^S12" => HL7MessageType.SIU_S12,
            "SIU^S13" => HL7MessageType.SIU_S13,
            "SIU^S14" => HL7MessageType.SIU_S14,
            "SIU^S15" => HL7MessageType.SIU_S15,
            "SIU^S16" => HL7MessageType.SIU_S16,
            "SIU^S17" => HL7MessageType.SIU_S17,
            "MDM^T01" => HL7MessageType.MDM_T01,
            "MDM^T02" => HL7MessageType.MDM_T02,
            "MDM^T03" => HL7MessageType.MDM_T03,
            "MDM^T04" => HL7MessageType.MDM_T04,
            "RAS^O17" => HL7MessageType.RAS_O17,
            "RDE^O11" => HL7MessageType.RDE_O11,
            "RDS^O13" => HL7MessageType.RDS_O13,
            "RGV^O15" => HL7MessageType.RGV_O15,
            "LAB^L01" => HL7MessageType.LAB_L01,
            "LAB^L02" => HL7MessageType.LAB_L02,
            "LAB^L03" => HL7MessageType.LAB_L03,
            _ => HL7MessageType.Unknown
        };
    }

    public static string GetDescription(this HL7MessageType messageType)
    {
        return messageType switch
        {
            HL7MessageType.ADT_A01 => "Admit/visit notification",
            HL7MessageType.ADT_A02 => "Transfer a patient",
            HL7MessageType.ADT_A03 => "Discharge/end visit",
            HL7MessageType.ADT_A04 => "Register a patient",
            HL7MessageType.ADT_A08 => "Update patient information",
            HL7MessageType.ORM_O01 => "Order message",
            HL7MessageType.ORU_R01 => "Observation result",
            HL7MessageType.ACK => "General acknowledgment",
            HL7MessageType.NAK => "General negative acknowledgment",
            _ => "Unknown message type"
        };
    }
}