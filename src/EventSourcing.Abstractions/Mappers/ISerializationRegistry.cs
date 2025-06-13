using System.Diagnostics.CodeAnalysis;
using FluentResults;

namespace EventSourcing.Mappers;

public interface ISerializationRegistry<TAggregate> where TAggregate : IAggregate
{
    Result<ISerializedEvent> Serialize(IEvent @event);
    Result<IEvent> Deserialize(string schema, string data);
}
