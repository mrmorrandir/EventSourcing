using EventSourcing.Projections;

namespace EventSourcing.Publishers;

public interface IPublisher<in TAggregate> : IProjector<TAggregate> where TAggregate : IAggregate { }