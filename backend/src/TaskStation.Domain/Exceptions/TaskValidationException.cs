namespace TaskStation.Domain.Exceptions;

public class TaskValidationException : DomainException
{
    public IReadOnlyList<string> Errors { get; }

    public TaskValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public TaskValidationException(string error)
        : this(new[] { error }) { }
}
