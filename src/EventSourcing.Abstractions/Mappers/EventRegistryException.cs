namespace EventSourcing.Abstractions.Mappers;

public class EventRegistryException : Exception
{
    public EventRegistryException(string message) : base(message) { }
}