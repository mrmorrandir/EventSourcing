namespace EventSourcing;

public interface IAggregate
{
    Guid Id { get; }
}