namespace EventSourcing.SourceGenerators.Target.TestEnvironment.Base;

public record Aggregate<T>(T Instance, int Version) where T : IAggregate;