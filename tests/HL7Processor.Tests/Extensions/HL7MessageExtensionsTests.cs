using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using FluentAssertions;

namespace HL7Processor.Tests.Extensions;

public class HL7MessageExtensionsTests
{
    [Fact]
    public void GetPatientId_WithValidPIDSegment_ShouldReturnPatientId()
    {
        // Arrange
        var message = new HL7Message();
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M");
        message.AddSegment(pidSegment);

        // Act
        var result = message.GetPatientId();

        // Assert
        result.Should().Be("123456789");
    }

    [Fact]
    public void GetPatientId_WithoutPIDSegment_ShouldReturnEmptyString()
    {
        // Arrange
        var message = new HL7Message();

        // Act
        var result = message.GetPatientId();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPatientName_WithValidPIDSegment_ShouldReturnFormattedName()
    {
        // Arrange
        var message = new HL7Message();
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M");
        message.AddSegment(pidSegment);

        // Act
        var result = message.GetPatientName();

        // Assert
        result.Should().Be("JOHN DOE");
    }

    [Fact]
    public void GetPatientName_WithoutPIDSegment_ShouldReturnEmptyString()
    {
        // Arrange
        var message = new HL7Message();

        // Act
        var result = message.GetPatientName();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPatientBirthDate_WithValidDate_ShouldReturnDateTime()
    {
        // Arrange
        var message = new HL7Message();
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M");
        message.AddSegment(pidSegment);

        // Act
        var result = message.GetPatientBirthDate();

        // Assert
        result.Should().Be(new DateTime(1980, 1, 1));
    }

    [Fact]
    public void GetPatientBirthDate_WithInvalidDate_ShouldReturnNull()
    {
        // Arrange
        var message = new HL7Message();
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||INVALID|M");
        message.AddSegment(pidSegment);

        // Act
        var result = message.GetPatientBirthDate();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPatientGender_WithValidPIDSegment_ShouldReturnGender()
    {
        // Arrange
        var message = new HL7Message();
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M");
        message.AddSegment(pidSegment);

        // Act
        var result = message.GetPatientGender();

        // Assert
        result.Should().Be("M");
    }

    [Fact]
    public void GetEventType_WithValidEVNSegment_ShouldReturnEventType()
    {
        // Arrange
        var message = new HL7Message();
        var evnSegment = new HL7Segment("EVN", "EVN|A01|20230101120000");
        message.AddSegment(evnSegment);

        // Act
        var result = message.GetEventType();

        // Assert
        result.Should().Be("A01");
    }

    [Fact]
    public void GetEventDateTime_WithValidEVNSegment_ShouldReturnDateTime()
    {
        // Arrange
        var message = new HL7Message();
        var evnSegment = new HL7Segment("EVN", "EVN|A01|20230101120000");
        message.AddSegment(evnSegment);

        // Act
        var result = message.GetEventDateTime();

        // Assert
        result.Should().Be(new DateTime(2023, 1, 1, 12, 0, 0));
    }

    [Fact]
    public void GetVisitNumber_WithValidPV1Segment_ShouldReturnVisitNumber()
    {
        // Arrange
        var message = new HL7Message();
        var pv1Segment = new HL7Segment("PV1", "PV1|1|I|ICU^101^A|E|||DOC123^SMITH^JANE^M^MD|ADM|SUR||||A|||DOC123^SMITH^JANE^M^MD|IP|12345|INS1");
        message.AddSegment(pv1Segment);

        // Act
        var result = message.GetVisitNumber();

        // Assert
        result.Should().Be("12345");
    }

    [Fact]
    public void GetAllergies_WithValidAL1Segments_ShouldReturnAllergies()
    {
        // Arrange
        var message = new HL7Message();
        var al1Segment1 = new HL7Segment("AL1", "AL1|1|DA|PENICILLIN|MO|RASH");
        var al1Segment2 = new HL7Segment("AL1", "AL1|2|DA|ASPIRIN|MO|NAUSEA");
        message.AddSegment(al1Segment1);
        message.AddSegment(al1Segment2);

        // Act
        var result = message.GetAllergies();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("PENICILLIN");
        result.Should().Contain("ASPIRIN");
    }

    [Fact]
    public void GetAllergies_WithoutAL1Segments_ShouldReturnEmptyList()
    {
        // Arrange
        var message = new HL7Message();

        // Act
        var result = message.GetAllergies();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void HasSegment_WithExistingSegment_ShouldReturnTrue()
    {
        // Arrange
        var message = new HL7Message();
        var pidSegment = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M");
        message.AddSegment(pidSegment);

        // Act
        var result = message.HasSegment("PID");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasSegment_WithoutSegment_ShouldReturnFalse()
    {
        // Arrange
        var message = new HL7Message();

        // Act
        var result = message.HasSegment("PID");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSegmentCount_WithMultipleSegments_ShouldReturnCorrectCount()
    {
        // Arrange
        var message = new HL7Message();
        var al1Segment1 = new HL7Segment("AL1", "AL1|1|DA|PENICILLIN|MO|RASH");
        var al1Segment2 = new HL7Segment("AL1", "AL1|2|DA|ASPIRIN|MO|NAUSEA");
        message.AddSegment(al1Segment1);
        message.AddSegment(al1Segment2);

        // Act
        var result = message.GetSegmentCount("AL1");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void ToJsonString_WithValidMessage_ShouldReturnJsonString()
    {
        // Arrange
        var message = new HL7Message
        {
            Id = "test-id",
            MessageType = HL7MessageType.ADT_A01,
            Version = "2.5"
        };

        // Act
        var result = message.ToJsonString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"id\": \"test-id\"");
        result.Should().Contain("\"messageType\": \"ADT_A01\"");
        result.Should().Contain("\"version\": \"2.5\"");
    }

    [Fact]
    public void FromJsonString_WithValidJson_ShouldReturnMessage()
    {
        // Arrange
        var json = @"{
            ""id"": ""test-id"",
            ""messageType"": ""ADT_A01"",
            ""version"": ""2.5"",
            ""rawMessage"": """",
            ""segments"": [],
            ""isValid"": false,
            ""validationErrors"": [],
            ""metadata"": {},
            ""timestamp"": ""2023-01-01T12:00:00Z""
        }";

        // Act
        var result = HL7MessageExtensions.FromJsonString(json);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("test-id");
        result.MessageType.Should().Be(HL7MessageType.ADT_A01);
        result.Version.Should().Be("2.5");
    }
}