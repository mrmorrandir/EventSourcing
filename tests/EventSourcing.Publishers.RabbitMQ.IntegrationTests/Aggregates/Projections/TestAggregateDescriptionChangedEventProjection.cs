// ReSharper disable once CheckNamespace
namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates.Repositories;

public partial class TestAggregateDescriptionChangedEventProjection
{
    public override Task<Result> ProjectAsync(TestAggregate state, DescriptionChangedEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}