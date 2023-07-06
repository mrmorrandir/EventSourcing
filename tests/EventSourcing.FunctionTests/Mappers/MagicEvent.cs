using System.Text.Json.Serialization;
using EventSourcing.Abstractions;
using EventSourcing.FunctionTests.Mappers.Events;

namespace EventSourcing.FunctionTests.Mappers;

public record MagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;