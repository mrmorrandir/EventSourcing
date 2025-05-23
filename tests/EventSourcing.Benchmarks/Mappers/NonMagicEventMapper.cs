using System.Text.Json;
using EventSourcing.Benchmarks.Events;
using EventSourcing.Mappers;

namespace EventSourcing.Benchmarks.Mappers;

public class NonMagicEventMapper : AbstractEventMapper<NonMagicEvent>
{
    public record NonMagicEventV1(Guid Id, DateTime Created);
    public record NonMagicEventV2(Guid Id, string MagicSpell, DateTime Created);
    
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