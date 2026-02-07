using TaskStation.Application.DTOs;
using TaskStation.Application.Validators;

namespace TaskStation.Tests.Application.Validators;

public class UpdateTaskRequestValidatorTests
{
    private readonly UpdateTaskRequestValidator _validator;

    public UpdateTaskRequestValidatorTests()
    {
        _validator = new UpdateTaskRequestValidator();
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "Updated Task",
            SlaHours = 48,
            Status = "PENDING"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("DONE")]
    [InlineData("pending")]
    [InlineData("done")]
    public async Task Validate_WithValidStatus_ShouldPass(string status)
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "Task",
            SlaHours = 24,
            Status = status
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyTitle_ShouldFail(string invalidTitle)
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = invalidTitle,
            SlaHours = 24,
            Status = "PENDING"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Validate_WithInvalidSlaHours_ShouldFail(int invalidSla)
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "Task",
            SlaHours = invalidSla,
            Status = "PENDING"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SlaHours");
    }

    [Fact]
    public async Task Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "",
            SlaHours = 0,
            Status = "INVALID"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Select(e => e.PropertyName).Should().Contain(new[] { "Title", "SlaHours", "Status" });
    }
}
