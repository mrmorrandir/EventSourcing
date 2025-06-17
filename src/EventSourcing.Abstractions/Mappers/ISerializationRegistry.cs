using System.Diagnostics.CodeAnalysis;
using FluentResults;

namespace EventSourcing.Mappers;

public interface ISerializationRegistry<in TAggregate> where TAggregate : IAggregate
{
    Result<ISerializedState> Serialize(TAggregate state);
    Result<ISerializedEvent> Serialize(IEvent @event);
    Result<IEvent> Deserialize(string schema, string data);
}
