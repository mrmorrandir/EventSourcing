using FluentResults;

namespace EventSourcing;

public interface IAggregator<TAggregate>
{
    Result<TAggregate> CreateFromEvent(IEvent evt);
    Result<TAggregate> ApplyEvent(TAggregate aggregate, IEvent evt);
}