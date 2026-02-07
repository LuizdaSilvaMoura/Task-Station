using TaskStation.Application.DTOs;
using TaskStation.Application.Validators;

namespace TaskStation.Tests.Application.Validators;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator;

    public CreateTaskRequestValidatorTests()
    {
        _validator = new CreateTaskRequestValidator();
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task",
            SlaHours = 24
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyTitle_ShouldFail(string invalidTitle)
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = invalidTitle,
            SlaHours = 24
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Validate_WithTitleTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = new string('a', 201),
            SlaHours = 24
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("200"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WithInvalidSlaHours_ShouldFail(int invalidSla)
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Title",
            SlaHours = invalidSla
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SlaHours");
    }

    [Fact]
    public async Task Validate_WithSlaTooHigh_ShouldFail()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Title",
            SlaHours = 8761 // Max is 8760
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SlaHours" && e.ErrorMessage.Contains("8760"));
    }

    [Fact]
    public async Task Validate_WithBoundaryValues_ShouldPass()
    {
        // Arrange
        var requests = new[]
        {
            new CreateTaskRequest { Title = "T", SlaHours = 1 }, // Min values
            new CreateTaskRequest { Title = new string('a', 200), SlaHours = 720 } // Max values
        };

        foreach (var request in requests)
        {
            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"Request with Title.Length={request.Title.Length} and SlaHours={request.SlaHours} should be valid");
        }
    }
}
