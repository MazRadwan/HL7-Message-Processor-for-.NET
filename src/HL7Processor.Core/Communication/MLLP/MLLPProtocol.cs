using HL7Processor.Core.Models;
using HL7Processor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HL7Processor.Core.Communication.MLLP;

/// <summary>
/// Minimal Lower Layer Protocol (MLLP) implementation for HL7 v2 message transmission
/// </summary>
public static class MLLPProtocol
{
    // MLLP control characters
    public const byte START_BLOCK = 0x0B;      // Vertical Tab (VT)
    public const byte END_BLOCK = 0x1C;        // File Separator (FS)
    public const byte CARRIAGE_RETURN = 0x0D;  // Carriage Return (CR)
    
    // MLLP frame delimiters
    public static readonly byte[] START_DELIMITER = { START_BLOCK };
    public static readonly byte[] END_DELIMITER = { END_BLOCK, CARRIAGE_RETURN };
    
    // Maximum message size (configurable)
    public const int DEFAULT_MAX_MESSAGE_SIZE = 1024 * 1024; // 1MB
    
    /// <summary>
    /// Wraps an HL7 message with MLLP framing
    /// </summary>
    public static byte[] WrapMessage(string hl7Message)
    {
        if (string.IsNullOrEmpty(hl7Message))
            throw new ArgumentException("HL7 message cannot be null or empty", nameof(hl7Message));

        var messageBytes = Encoding.UTF8.GetBytes(hl7Message);
        var wrappedMessage = new byte[messageBytes.Length + 3]; // Start + Message + End + CR

        wrappedMessage[0] = START_BLOCK;
        Array.Copy(messageBytes, 0, wrappedMessage, 1, messageBytes.Length);
        wrappedMessage[wrappedMessage.Length - 2] = END_BLOCK;
        wrappedMessage[wrappedMessage.Length - 1] = CARRIAGE_RETURN;

        return wrappedMessage;
    }

    /// <summary>
    /// Unwraps an MLLP-framed message to extract the HL7 content
    /// </summary>
    public static string UnwrapMessage(byte[] mllpMessage)
    {
        if (mllpMessage == null || mllpMessage.Length < 3)
            throw new ArgumentException("Invalid MLLP message format", nameof(mllpMessage));

        if (mllpMessage[0] != START_BLOCK)
            throw new MLLPException("Missing MLLP start block");

        if (mllpMessage[mllpMessage.Length - 2] != END_BLOCK || 
            mllpMessage[mllpMessage.Length - 1] != CARRIAGE_RETURN)
            throw new MLLPException("Missing MLLP end delimiter");

        var messageLength = mllpMessage.Length - 3;
        var messageBytes = new byte[messageLength];
        Array.Copy(mllpMessage, 1, messageBytes, 0, messageLength);

        return Encoding.UTF8.GetString(messageBytes);
    }

    /// <summary>
    /// Validates MLLP message format
    /// </summary>
    public static MLLPValidationResult ValidateMessage(byte[] mllpMessage)
    {
        var result = new MLLPValidationResult { IsValid = true };

        if (mllpMessage == null)
        {
            result.AddError("MLLP message cannot be null");
            return result;
        }

        if (mllpMessage.Length < 3)
        {
            result.AddError("MLLP message too short (minimum 3 bytes required)");
            return result;
        }

        if (mllpMessage[0] != START_BLOCK)
        {
            result.AddError($"Invalid start block: expected 0x{START_BLOCK:X2}, got 0x{mllpMessage[0]:X2}");
        }

        if (mllpMessage.Length >= 2 && mllpMessage[mllpMessage.Length - 2] != END_BLOCK)
        {
            result.AddError($"Invalid end block: expected 0x{END_BLOCK:X2}, got 0x{mllpMessage[mllpMessage.Length - 2]:X2}");
        }

        if (mllpMessage[mllpMessage.Length - 1] != CARRIAGE_RETURN)
        {
            result.AddError($"Invalid end delimiter: expected 0x{CARRIAGE_RETURN:X2}, got 0x{mllpMessage[mllpMessage.Length - 1]:X2}");
        }

        if (mllpMessage.Length > DEFAULT_MAX_MESSAGE_SIZE)
        {
            result.AddWarning($"Message size ({mllpMessage.Length} bytes) exceeds recommended maximum ({DEFAULT_MAX_MESSAGE_SIZE} bytes)");
        }

        return result;
    }

    /// <summary>
    /// Checks if a byte array contains a complete MLLP message
    /// </summary>
    public static bool IsCompleteMessage(byte[] buffer, int length)
    {
        if (buffer == null || length < 3)
            return false;

        // Look for start block
        var startIndex = -1;
        for (int i = 0; i < length; i++)
        {
            if (buffer[i] == START_BLOCK)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex == -1)
            return false;

        // Look for end delimiter starting from the position after start block
        for (int i = startIndex + 1; i < length - 1; i++)
        {
            if (buffer[i] == END_BLOCK && buffer[i + 1] == CARRIAGE_RETURN)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts a complete MLLP message from a buffer
    /// </summary>
    public static MLLPMessageResult ExtractMessage(byte[] buffer, int length)
    {
        var result = new MLLPMessageResult();

        if (buffer == null || length < 3)
        {
            result.IsComplete = false;
            return result;
        }

        // Find start block
        var startIndex = -1;
        for (int i = 0; i < length; i++)
        {
            if (buffer[i] == START_BLOCK)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex == -1)
        {
            result.IsComplete = false;
            return result;
        }

        // Find end delimiter
        for (int i = startIndex + 1; i < length - 1; i++)
        {
            if (buffer[i] == END_BLOCK && buffer[i + 1] == CARRIAGE_RETURN)
            {
                var messageLength = i - startIndex + 2;
                result.MessageBytes = new byte[messageLength];
                Array.Copy(buffer, startIndex, result.MessageBytes, 0, messageLength);
                result.IsComplete = true;
                result.BytesConsumed = i + 2;
                return result;
            }
        }

        result.IsComplete = false;
        return result;
    }

    /// <summary>
    /// Creates an MLLP acknowledgment message
    /// </summary>
    public static byte[] CreateAcknowledgment(string messageControlId, AcknowledgmentCode ackCode, string? errorMessage = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var ackType = ackCode switch
        {
            AcknowledgmentCode.ApplicationAccept => "AA",
            AcknowledgmentCode.ApplicationError => "AE",
            AcknowledgmentCode.ApplicationReject => "AR",
            AcknowledgmentCode.CommitAccept => "CA",
            AcknowledgmentCode.CommitError => "CE",
            AcknowledgmentCode.CommitReject => "CR",
            _ => "AA"
        };

        var mshSegment = $"MSH|^~\\&|HL7Processor|Local|Sender|Remote|{timestamp}||ACK^A01|{messageControlId}|P|2.5";
        var msaSegment = $"MSA|{ackType}|{messageControlId}";
        
        var ackMessage = mshSegment + "\r" + msaSegment;

        if (!string.IsNullOrEmpty(errorMessage))
        {
            var errSegment = $"ERR|||{errorMessage}";
            ackMessage += "\r" + errSegment;
        }

        return WrapMessage(ackMessage);
    }

    /// <summary>
    /// Parses an MLLP acknowledgment message
    /// </summary>
    public static MLLPAcknowledgment ParseAcknowledgment(byte[] mllpMessage)
    {
        var hl7Message = UnwrapMessage(mllpMessage);
        var segments = hl7Message.Split('\r');

        var acknowledgment = new MLLPAcknowledgment();

        foreach (var segment in segments)
        {
            var fields = segment.Split('|');
            
            switch (fields[0])
            {
                case "MSH":
                    if (fields.Length > 9)
                        acknowledgment.MessageControlId = fields[9];
                    break;
                    
                case "MSA":
                    if (fields.Length > 1)
                    {
                        acknowledgment.AcknowledgmentCode = fields[1] switch
                        {
                            "AA" => AcknowledgmentCode.ApplicationAccept,
                            "AE" => AcknowledgmentCode.ApplicationError,
                            "AR" => AcknowledgmentCode.ApplicationReject,
                            "CA" => AcknowledgmentCode.CommitAccept,
                            "CE" => AcknowledgmentCode.CommitError,
                            "CR" => AcknowledgmentCode.CommitReject,
                            _ => AcknowledgmentCode.ApplicationAccept
                        };
                    }
                    if (fields.Length > 2)
                        acknowledgment.OriginalMessageControlId = fields[2];
                    break;
                    
                case "ERR":
                    if (fields.Length > 3)
                        acknowledgment.ErrorMessage = fields[3];
                    break;
            }
        }

        return acknowledgment;
    }
}

public class MLLPValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

public class MLLPMessageResult
{
    public bool IsComplete { get; set; }
    public byte[]? MessageBytes { get; set; }
    public int BytesConsumed { get; set; }
}

public class MLLPAcknowledgment
{
    public string MessageControlId { get; set; } = string.Empty;
    public string OriginalMessageControlId { get; set; } = string.Empty;
    public AcknowledgmentCode AcknowledgmentCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum AcknowledgmentCode
{
    ApplicationAccept,    // AA
    ApplicationError,     // AE
    ApplicationReject,    // AR
    CommitAccept,        // CA
    CommitError,         // CE
    CommitReject         // CR
}

public class MLLPException : HL7ProcessingException
{
    public MLLPException(string message) : base(message, null, null, null)
    {
    }

    public MLLPException(string message, Exception innerException) : base(message, innerException, null, null)
    {
    }
}