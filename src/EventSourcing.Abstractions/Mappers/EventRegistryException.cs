namespace EventSourcing.Abstractions.Mappers;

public class EventRegistryException : Exception
{
    public EventRegistryException() { }
    public EventRegistryException(string message) : base(message) { }
    public EventRegistryException(string message, Exception inner) : base(message, inner) { }
}