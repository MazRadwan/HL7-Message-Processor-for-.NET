using HL7Processor.Core.Parsing;
using HL7Processor.Core.Models;
using HL7Processor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace HL7Processor.Tests.Parsing;

public class HL7ParserTests
{
    private readonly Mock<ILogger<HL7Parser>> _loggerMock;
    private readonly HL7Parser _parser;

    public HL7ParserTests()
    {
        _loggerMock = new Mock<ILogger<HL7Parser>>();
        _parser = new HL7Parser(_loggerMock.Object);
    }

    [Fact]
    public void Parse_ValidADTMessage_ShouldReturnParsedMessage()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5
EVN|A01|20230101120000
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789
PV1|1|I|ICU^101^A|E|||DOC123^SMITH^JANE^M^MD|ADM|SUR||||A|||DOC123^SMITH^JANE^M^MD|IP|12345|INS1";

        // Act
        var result = _parser.Parse(rawMessage);

        // Assert
        result.Should().NotBeNull();
        result.MessageType.Should().Be(HL7MessageType.ADT_A01);
        result.Version.Should().Be("2.5");
        result.MessageControlId.Should().Be("12345");
        result.Segments.Should().HaveCount(4);
        result.Segments.Should().Contain(s => s.Type == "MSH");
        result.Segments.Should().Contain(s => s.Type == "EVN");
        result.Segments.Should().Contain(s => s.Type == "PID");
        result.Segments.Should().Contain(s => s.Type == "PV1");
    }

    [Fact]
    public void Parse_ValidORMMessage_ShouldReturnParsedMessage()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ORM^O01|12345|P|2.5
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789
ORC|NW|12345|67890|||||20230101120000||DOC123^SMITH^JANE^M^MD
OBR|1|12345|67890|CBC^Complete Blood Count^L||20230101120000|20230101120000|||||Lab Notes||DOC123^SMITH^JANE^M^MD";

        // Act
        var result = _parser.Parse(rawMessage);

        // Assert
        result.Should().NotBeNull();
        result.MessageType.Should().Be(HL7MessageType.ORM_O01);
        result.Version.Should().Be("2.5");
        result.MessageControlId.Should().Be("12345");
        result.Segments.Should().HaveCount(4);
        result.Segments.Should().Contain(s => s.Type == "MSH");
        result.Segments.Should().Contain(s => s.Type == "PID");
        result.Segments.Should().Contain(s => s.Type == "ORC");
        result.Segments.Should().Contain(s => s.Type == "OBR");
    }

    [Fact]
    public void Parse_EmptyMessage_ShouldThrowException()
    {
        // Arrange
        var rawMessage = "";

        // Act & Assert
        var exception = Assert.Throws<HL7ProcessingException>(() => _parser.Parse(rawMessage));
        exception.Message.Should().Contain("Raw message cannot be null or empty");
    }

    [Fact]
    public void Parse_NullMessage_ShouldThrowException()
    {
        // Arrange
        string rawMessage = null!;

        // Act & Assert
        var exception = Assert.Throws<HL7ProcessingException>(() => _parser.Parse(rawMessage));
        exception.Message.Should().Contain("Raw message cannot be null or empty");
    }

    [Fact]
    public void Parse_MessageWithoutMSH_ShouldAddValidationError()
    {
        // Arrange
        var rawMessage = @"EVN|A01|20230101120000
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789";

        // Act
        var result = _parser.Parse(rawMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("MSH (Message Header) segment is required");
    }

    [Fact]
    public void Parse_MessageWithInvalidSegmentType_ShouldThrowException()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5
XX|A01|20230101120000";

        // Act & Assert
        var exception = Assert.Throws<HL7ProcessingException>(() => _parser.Parse(rawMessage));
        exception.Message.Should().Contain("Segment data is too short");
    }

    [Fact]
    public void TryParse_ValidMessage_ShouldReturnTrueAndParsedMessage()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5
EVN|A01|20230101120000
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789";

        // Act
        var success = _parser.TryParse(rawMessage, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.MessageType.Should().Be(HL7MessageType.ADT_A01);
    }

    [Fact]
    public void TryParse_InvalidMessage_ShouldReturnFalseAndNullMessage()
    {
        // Arrange
        var rawMessage = "";

        // Act
        var success = _parser.TryParse(rawMessage, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void SerializeToHL7_ValidMessage_ShouldReturnHL7String()
    {
        // Arrange
        var message = new HL7Message
        {
            MessageType = HL7MessageType.ADT_A01,
            Version = "2.5"
        };

        var mshSegment = new HL7Segment("MSH", "MSH|^~\\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5");
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789");
        
        message.AddSegment(mshSegment);
        message.AddSegment(pidSegment);

        // Act
        var result = _parser.SerializeToHL7(message);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("MSH|");
        result.Should().Contain("PID|");
        result.Should().Contain("\r");
    }

    [Fact]
    public void SerializeToHL7_NullMessage_ShouldThrowException()
    {
        // Arrange
        HL7Message message = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _parser.SerializeToHL7(message));
        exception.ParamName.Should().Be("message");
    }

    [Fact]
    public void Parse_MessageWithHL7DateTime_ShouldParseDateTimeCorrectly()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5
EVN|A01|20230101120000
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789";

        // Act
        var result = _parser.Parse(rawMessage);

        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().Be(new DateTime(2023, 1, 1, 12, 0, 0));
    }

    [Fact]
    public void Parse_MessageWithUnknownMessageType_ShouldSetMessageTypeToUnknown()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||XXX^Y01|12345|P|2.5
EVN|A01|20230101120000
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789";

        // Act
        var result = _parser.Parse(rawMessage);

        // Assert
        result.Should().NotBeNull();
        result.MessageType.Should().Be(HL7MessageType.Unknown);
    }

    [Fact]
    public void Parse_MessageWithMultipleSegments_ShouldParseAllSegments()
    {
        // Arrange
        var rawMessage = @"MSH|^~\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5
EVN|A01|20230101120000
PID|1||123456789||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345||555-1234|555-5678|EN|S|CHR|123456789
PV1|1|I|ICU^101^A|E|||DOC123^SMITH^JANE^M^MD|ADM|SUR||||A|||DOC123^SMITH^JANE^M^MD|IP|12345|INS1
AL1|1|DA|PENICILLIN|MO|RASH
AL1|2|DA|ASPIRIN|MO|NAUSEA";

        // Act
        var result = _parser.Parse(rawMessage);

        // Assert
        result.Should().NotBeNull();
        result.Segments.Should().HaveCount(6);
        result.GetSegments("AL1").Should().HaveCount(2);
    }
}