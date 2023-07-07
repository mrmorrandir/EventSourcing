namespace EventSourcing.Abstractions.Mappers;

public class EventDeserializerException : Exception
{
    public EventDeserializerException(string message, Exception inner) : base(message, inner) { }
}