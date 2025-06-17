using System.Diagnostics.CodeAnalysis;
using FluentResults;

namespace EventSourcing.Mappers;

public interface ISerializationRegistry<in TAggregate> where TAggregate : IAggregate
{
    Result<ISerializedState> Serialize(TAggregate state);
    // TODO: The SourceGenerator should generate this interface, so you can have a lot of overloads for different types of aggregates. (spares the switch statement)
    Result<ISerializedEvent> Serialize(IEvent @event);
    Result<IEvent> Deserialize(string schema, string data);
}
