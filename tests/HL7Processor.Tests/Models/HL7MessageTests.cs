using HL7Processor.Core.Models;
using FluentAssertions;

namespace HL7Processor.Tests.Models;

public class HL7MessageTests
{
    [Fact]
    public void HL7Message_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var message = new HL7Message();

        // Assert
        message.Id.Should().NotBeNullOrEmpty();
        message.MessageType.Should().Be(HL7MessageType.Unknown);
        message.Version.Should().Be("2.5");
        message.RawMessage.Should().BeEmpty();
        message.Segments.Should().BeEmpty();
        message.IsValid.Should().BeFalse();
        message.ValidationErrors.Should().BeEmpty();
        message.Metadata.Should().BeEmpty();
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddValidationError_ShouldAddErrorAndSetInvalid()
    {
        // Arrange
        var message = new HL7Message();
        var error = "Test validation error";

        // Act
        message.AddValidationError(error);

        // Assert
        message.ValidationErrors.Should().Contain(error);
        message.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClearValidationErrors_ShouldClearErrorsAndSetValid()
    {
        // Arrange
        var message = new HL7Message();
        message.AddValidationError("Test error");

        // Act
        message.ClearValidationErrors();

        // Assert
        message.ValidationErrors.Should().BeEmpty();
        message.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetSegment_ExistingSegment_ShouldReturnSegment()
    {
        // Arrange
        var message = new HL7Message();
        var segment = new HL7Segment("MSH", "MSH|^~\\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5");
        message.AddSegment(segment);

        // Act
        var result = message.GetSegment("MSH");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(segment);
    }

    [Fact]
    public void GetSegment_NonExistingSegment_ShouldReturnNull()
    {
        // Arrange
        var message = new HL7Message();

        // Act
        var result = message.GetSegment("PID");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSegments_ExistingSegmentType_ShouldReturnAllSegments()
    {
        // Arrange
        var message = new HL7Message();
        var segment1 = new HL7Segment("AL1", "AL1|1|DA|PENICILLIN|MO|RASH");
        var segment2 = new HL7Segment("AL1", "AL1|2|DA|ASPIRIN|MO|NAUSEA");
        message.AddSegment(segment1);
        message.AddSegment(segment2);

        // Act
        var result = message.GetSegments("AL1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(segment1);
        result.Should().Contain(segment2);
    }

    [Fact]
    public void AddSegment_ValidSegment_ShouldAddToSegments()
    {
        // Arrange
        var message = new HL7Message();
        var segment = new HL7Segment("MSH", "MSH|^~\\&|SENDING_APP|SENDING_FAC|RECEIVING_APP|RECEIVING_FAC|20230101120000||ADT^A01|12345|P|2.5");

        // Act
        message.AddSegment(segment);

        // Assert
        message.Segments.Should().Contain(segment);
        message.Segments.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveSegment_ExistingSegment_ShouldRemoveAllSegmentsOfType()
    {
        // Arrange
        var message = new HL7Message();
        var segment1 = new HL7Segment("AL1", "AL1|1|DA|PENICILLIN|MO|RASH");
        var segment2 = new HL7Segment("AL1", "AL1|2|DA|ASPIRIN|MO|NAUSEA");
        var segment3 = new HL7Segment("PID", "PID|1||123456789||DOE^JOHN^M||19800101|M");
        message.AddSegment(segment1);
        message.AddSegment(segment2);
        message.AddSegment(segment3);

        // Act
        message.RemoveSegment("AL1");

        // Assert
        message.Segments.Should().HaveCount(1);
        message.Segments.Should().Contain(segment3);
        message.Segments.Should().NotContain(segment1);
        message.Segments.Should().NotContain(segment2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var message = new HL7Message
        {
            Id = "test-id",
            MessageType = HL7MessageType.ADT_A01,
            Version = "2.5",
            IsValid = true
        };

        // Act
        var result = message.ToString();

        // Assert
        result.Should().Be("HL7Message [ADT^A01] - ID: test-id, Version: 2.5, Valid: True");
    }
}