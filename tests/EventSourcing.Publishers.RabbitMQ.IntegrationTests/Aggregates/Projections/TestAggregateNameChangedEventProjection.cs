// ReSharper disable once CheckNamespace
namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates.Repositories;

public partial class TestAggregateNameChangedEventProjection
{
    public override Task<Result> ProjectAsync(TestAggregate state, NameChangedEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}