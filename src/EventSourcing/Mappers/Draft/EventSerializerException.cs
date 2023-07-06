namespace EventSourcing.Mappers;

public class EventSerializerException : Exception
{
    public EventSerializerException() { }
    public EventSerializerException(string message) : base(message) { }
    public EventSerializerException(string message, Exception inner) : base(message, inner) { }
}