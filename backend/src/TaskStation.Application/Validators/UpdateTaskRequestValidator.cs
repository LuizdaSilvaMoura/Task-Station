using FluentValidation;
using TaskStation.Application.DTOs;

namespace TaskStation.Application.Validators;

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.SlaHours)
            .GreaterThan(0).WithMessage("SlaHours must be greater than 0")
            .LessThanOrEqualTo(720).WithMessage("SlaHours cannot exceed 720 (30 days)");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => status.ToUpperInvariant() is "PENDING" or "DONE")
            .WithMessage("Status must be either PENDING or DONE");
    }
}
