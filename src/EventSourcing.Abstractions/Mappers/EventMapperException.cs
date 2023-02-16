namespace EventSourcing.Abstractions.Mappers;

public class EventMapperException : Exception
{
    public EventMapperException()
    {
    }

    public EventMapperException(string message) : base(message)
    {
    }

    public EventMapperException(string message, Exception inner) : base(message, inner)
    {
    }
}