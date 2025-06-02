using EventSourcing.Mappers;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public interface ISerializationRegistry<TAggregate> where TAggregate : IAggregate
{
    Result<ISerializedEvent> Serialize(IEvent @event);
    Result<IEvent> Deserialize(string schema, string data);
}