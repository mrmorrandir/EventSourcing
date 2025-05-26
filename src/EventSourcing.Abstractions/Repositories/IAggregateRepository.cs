namespace EventSourcing.Repositories;

public interface IAggregateRepository<out TAggregate> where TAggregate : IAggregate
{
    TAggregate Get(Guid id);
    void Save(Guid id, IEnumerable<IEvent> events);
    TAggregate SaveAndGet(Guid id, IEnumerable<IEvent> events);
}