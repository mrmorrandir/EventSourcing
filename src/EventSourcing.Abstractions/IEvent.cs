namespace EventSourcing;

public interface IEvent
{
    Guid Id { get; }
}