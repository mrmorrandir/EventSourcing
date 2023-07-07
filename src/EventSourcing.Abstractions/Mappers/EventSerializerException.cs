namespace EventSourcing.Abstractions.Mappers;

public class EventSerializerException : Exception
{
    public EventSerializerException(string message, Exception inner) : base(message, inner) { }
}