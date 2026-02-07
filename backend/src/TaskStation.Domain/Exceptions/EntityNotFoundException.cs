namespace TaskStation.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, string id)
        : base($"{entityName} with id '{id}' was not found.") { }
}
