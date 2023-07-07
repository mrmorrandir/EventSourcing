using System.Text.Json;
using EventSourcing.Abstractions;
using EventSourcing.Mappers;

namespace EventSourcing.Benchmarks;

public class NonMagicEventMapper : AbstractEventMapper<NonMagicEvent>
{
    public record NonMagicEventV1(Guid Id, DateTime Created) : IEvent;
    public record NonMagicEventV2(Guid Id, string MagicSpell, DateTime Created) : IEvent;
    
    public NonMagicEventMapper()
    {
        WillSerialize("non-magic-event-v3");
        CanDeserialize("non-magic-event-v3");
        
        // V1
        CanDeserialize("non-magic-event", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<NonMagicEventV1>(data, options)!;
            return new NonMagicEvent(magicEvent.Id, string.Empty, magicEvent.Created);
        });

        // V1
        CanDeserialize("non-magic-event-v2", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<NonMagicEventV2>(data, options)!;
            return new NonMagicEvent(magicEvent.Id, magicEvent.MagicSpell, magicEvent.Created);
        });
        
    }
}