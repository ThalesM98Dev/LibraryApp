namespace LibraryApp.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, int id)
        : base($"{entity} with ID {id} was not found.") { }
}