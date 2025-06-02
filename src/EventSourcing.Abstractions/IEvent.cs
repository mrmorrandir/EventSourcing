namespace EventSourcing;

public interface IEvent
{
    Guid AggregateId { get; }
}