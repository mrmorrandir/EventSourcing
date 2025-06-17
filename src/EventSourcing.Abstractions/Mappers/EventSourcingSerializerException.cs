namespace EventSourcing.Mappers;

public class EventSourcingSerializerException : Exception
{
    public EventSourcingSerializerException(string message) : base(message) { }
    public EventSourcingSerializerException(string message, Exception inner) : base(message, inner) { }
}

