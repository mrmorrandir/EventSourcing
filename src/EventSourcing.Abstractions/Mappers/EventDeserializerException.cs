namespace EventSourcing.Abstractions.Mappers;

public class EventDeserializerException : Exception
{
    public EventDeserializerException() { }
    public EventDeserializerException(string message) : base(message) { }
    public EventDeserializerException(string message, Exception inner) : base(message, inner) { }
}