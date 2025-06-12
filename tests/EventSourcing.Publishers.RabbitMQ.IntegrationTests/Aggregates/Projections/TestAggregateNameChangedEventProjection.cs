// ReSharper disable once CheckNamespace
namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates.Repositories;

public partial class TestAggregateNameChangedEventProjection
{
    public override Task ProjectAsync(TestAggregate state, NameChangedEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}