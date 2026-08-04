// ReSharper disable once CheckNamespace
namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates.Repositories;

public partial class TestAggregateCreatedEventProjection
{
    public override Task<Result> ProjectAsync(TestAggregate state, CreatedEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}