using EventSourcing.Mappers;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public abstract class SerializationRegistry<TAggregate> : ISerializationRegistry<TAggregate> where TAggregate : IAggregate
{
    public abstract Result<ISerializedEvent> Serialize(IEvent @event);
    public abstract Result<IEvent> Deserialize(string schema, string data);
}