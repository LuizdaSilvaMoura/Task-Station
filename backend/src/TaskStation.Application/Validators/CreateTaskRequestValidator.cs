using FluentValidation;
using TaskStation.Application.DTOs;

namespace TaskStation.Application.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    private static readonly string[] AllowedExtensions =
        [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx", ".txt", ".zip"];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.SlaHours)
            .GreaterThan(0).WithMessage("SLA hours must be greater than zero.")
            .LessThanOrEqualTo(8760).WithMessage("SLA hours must not exceed 8760 (1 year).");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

            RuleFor(x => x.File!.FileName)
                .Must(HaveAllowedExtension)
                .WithMessage($"Allowed file extensions: {string.Join(", ", AllowedExtensions)}");
        });
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}
