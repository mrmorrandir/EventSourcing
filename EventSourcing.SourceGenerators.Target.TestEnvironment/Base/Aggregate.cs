namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public record Aggregate<T>(T Instance, int Version) where T : IAggregate;