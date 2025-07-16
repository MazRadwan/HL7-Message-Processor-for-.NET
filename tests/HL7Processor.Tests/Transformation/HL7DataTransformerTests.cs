using HL7Processor.Core.Transformation;
using HL7Processor.Core.Models;
using HL7Processor.Core.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace HL7Processor.Tests.Transformation;

public class HL7DataTransformerTests
{
    private readonly Mock<ILogger<HL7DataTransformer>> _loggerMock;
    private readonly HL7DataTransformer _transformer;

    public HL7DataTransformerTests()
    {
        _loggerMock = new Mock<ILogger<HL7DataTransformer>>();
        _transformer = new HL7DataTransformer(_loggerMock.Object);
    }

    [Fact]
    public void TransformMessage_WithValidMapping_ShouldReturnTransformedData()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = CreateTestMappingConfiguration();

        // Act
        var result = _transformer.TransformMessage(message, mappingConfig);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("patientId");
        result.Should().ContainKey("patientName");
        result.Should().ContainKey("messageType");
        result["patientId"].Should().Be("12345");
        result["patientName"].Should().Be("JOHN DOE");
        result["messageType"].Should().Be("ADT_A01");
    }

    [Fact]
    public void TransformMessage_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mappingConfig = CreateTestMappingConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _transformer.TransformMessage(null!, mappingConfig));
        exception.ParamName.Should().Be("message");
    }

    [Fact]
    public void TransformMessage_WithNullMappingConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _transformer.TransformMessage(message, null!));
        exception.ParamName.Should().Be("mappingConfig");
    }

    [Fact]
    public void TransformMessage_WithTransformFunction_ShouldApplyTransformation()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Transform Function",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-5",
                    TargetField = "patientNameUpper",
                    TransformFunction = "uppercase"
                }
            }
        };

        // Act
        var result = _transformer.TransformMessage(message, mappingConfig);

        // Assert
        result.Should().ContainKey("patientNameUpper");
        result["patientNameUpper"].Should().Be("DOE^JOHN^M");
    }

    [Fact]
    public void TransformMessage_WithDefaultValue_ShouldUseDefaultForMissingFields()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Default Value",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-99", // Non-existent field
                    TargetField = "missingField",
                    DefaultValue = "DEFAULT_VALUE",
                    IsRequired = true
                }
            }
        };

        // Act
        var result = _transformer.TransformMessage(message, mappingConfig);

        // Assert
        result.Should().ContainKey("missingField");
        result["missingField"].Should().Be("DEFAULT_VALUE");
    }

    [Fact]
    public void TransformMessage_WithDataTypeConversion_ShouldConvertToTargetType()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Data Type Conversion",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-7", // Birth date
                    TargetField = "birthDate",
                    DataType = "datetime"
                }
            }
        };

        // Act
        var result = _transformer.TransformMessage(message, mappingConfig);

        // Assert
        result.Should().ContainKey("birthDate");
        result["birthDate"].Should().BeOfType<DateTime>();
    }

    [Fact]
    public void TransformMessage_WithConditionalMapping_ShouldApplyBasedOnCondition()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Conditional Mapping",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-3",
                    TargetField = "patientId",
                    Conditions = new List<MappingCondition>
                    {
                        new MappingCondition
                        {
                            Field = "MessageType",
                            Operator = "equals",
                            Value = "ADT_A01"
                        }
                    }
                }
            }
        };

        // Act
        var result = _transformer.TransformMessage(message, mappingConfig);

        // Assert
        result.Should().ContainKey("patientId");
        result["patientId"].Should().Be("12345");
    }

    [Fact]
    public void TransformSegment_WithValidSegment_ShouldReturnTransformedData()
    {
        // Arrange
        var segment = new HL7Segment("PID", "PID|1||12345||DOE^JOHN^M||19800101|M");
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Segment Transform",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-3",
                    TargetField = "patientId"
                },
                new FieldMapping
                {
                    SourceField = "PID-5",
                    TargetField = "patientName"
                }
            }
        };

        // Act
        var result = _transformer.TransformSegment(segment, mappingConfig);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("patientId");
        result.Should().ContainKey("patientName");
        result["patientId"].Should().Be("12345");
        result["patientName"].Should().Be("DOE^JOHN^M");
    }

    [Fact]
    public void TransformMessages_WithMultipleMessages_ShouldReturnTransformedList()
    {
        // Arrange
        var messages = new List<HL7Message>
        {
            CreateTestMessage(),
            CreateTestMessage()
        };
        var mappingConfig = CreateTestMappingConfiguration();

        // Act
        var result = _transformer.TransformMessages(messages, mappingConfig);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Should().ContainKey("patientId"));
        result.Should().AllSatisfy(r => r.Should().ContainKey("patientName"));
    }

    [Fact]
    public void TransformToQueryable_WithMessages_ShouldReturnQueryableResult()
    {
        // Arrange
        var messages = new List<HL7Message>
        {
            CreateTestMessage(),
            CreateTestMessage()
        };
        var mappingConfig = CreateTestMappingConfiguration();

        // Act
        var result = _transformer.TransformToQueryable(messages, mappingConfig);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        var filteredResult = result.Where(r => r.ContainsKey("patientId")).ToList();
        filteredResult.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyFieldLevelTransformations_WithCustomRules_ShouldApplyTransformations()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        };

        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Field Transform",
            CustomRules = new List<CustomMappingRule>
            {
                new CustomMappingRule
                {
                    Name = "ConcatenateFullName",
                    RuleType = "field_transform",
                    AppliesTo = new List<string> { "fullName" },
                    Expression = "firstName + ' ' + lastName",
                    IsActive = true
                }
            }
        };

        // Act
        var result = _transformer.ApplyFieldLevelTransformations(data, mappingConfig);

        // Assert
        result.Should().ContainKey("fullName");
        result["fullName"].Should().Be("John Doe");
    }

    [Fact]
    public void TransformMessage_WithValidationPattern_ShouldValidateFieldValues()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Validation Pattern",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-3",
                    TargetField = "patientId",
                    ValidationPattern = @"^\d{5}$" // 5 digits
                }
            }
        };

        // Act
        var result = _transformer.TransformMessage(message, mappingConfig);

        // Assert
        result.Should().ContainKey("patientId");
        result["patientId"].Should().Be("12345");
    }

    [Fact]
    public void TransformMessage_WithGenericType_ShouldReturnTypedObject()
    {
        // Arrange
        var message = CreateTestMessage();
        var mappingConfig = new FieldMappingConfiguration
        {
            Name = "Test Generic Transform",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-3",
                    TargetField = "PatientId"
                },
                new FieldMapping
                {
                    SourceField = "PID-5",
                    TargetField = "PatientName"
                }
            }
        };

        // Act
        var result = _transformer.TransformMessage<TestPatient>(message, mappingConfig);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestPatient>();
        result.PatientId.Should().Be("12345");
        result.PatientName.Should().Be("DOE^JOHN^M");
    }

    private HL7Message CreateTestMessage()
    {
        var message = new HL7Message
        {
            Id = "TEST-123",
            MessageType = HL7MessageType.ADT_A01,
            Version = "2.5",
            SendingApplication = "TEST_APP",
            SendingFacility = "TEST_FAC",
            ReceivingApplication = "REC_APP",
            ReceivingFacility = "REC_FAC",
            MessageControlId = "MSG123",
            ProcessingId = "P",
            Timestamp = DateTime.Now
        };

        // Add MSH segment
        var mshSegment = new HL7Segment("MSH", "MSH|^~\\&|TEST_APP|TEST_FAC|REC_APP|REC_FAC|20230101120000||ADT^A01|MSG123|P|2.5");
        message.AddSegment(mshSegment);

        // Add PID segment
        var pidSegment = new HL7Segment("PID", "PID|1||12345||DOE^JOHN^M||19800101|M");
        message.AddSegment(pidSegment);

        // Add PV1 segment
        var pv1Segment = new HL7Segment("PV1", "PV1|1|I|ICU^101^A|E|||DOC123^SMITH^JANE^M^MD|ADM|SUR||||A|||DOC123^SMITH^JANE^M^MD|IP|V123|INS1");
        message.AddSegment(pv1Segment);

        return message;
    }

    private FieldMappingConfiguration CreateTestMappingConfiguration()
    {
        return new FieldMappingConfiguration
        {
            Name = "Test Mapping Configuration",
            Version = "1.0",
            Mappings = new List<FieldMapping>
            {
                new FieldMapping
                {
                    SourceField = "PID-3",
                    TargetField = "patientId",
                    DataType = "string",
                    IsRequired = true
                },
                new FieldMapping
                {
                    SourceField = "PID-5",
                    TargetField = "patientName",
                    DataType = "string",
                    TransformFunction = "uppercase"
                },
                new FieldMapping
                {
                    SourceField = "MessageType",
                    TargetField = "messageType",
                    DataType = "string"
                },
                new FieldMapping
                {
                    SourceField = "PID-7",
                    TargetField = "birthDate",
                    DataType = "date"
                },
                new FieldMapping
                {
                    SourceField = "PID-8",
                    TargetField = "gender",
                    DataType = "string"
                }
            }
        };
    }

    private class TestPatient
    {
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
    }
}