using System.Text.Json;
using EventSourcing.Mappers;

namespace EventSourcing.Benchmarks;

public class DoubleMagicEventMapper : AbstractEventMapper<DoubleMagicEvent>
{
    public record DoubleMagicEventV1(Guid Id, DateTime Created) : IEvent;
    public record DoubleMagicEventV2(Guid Id, string MagicSpell, DateTime Created) : IEvent;
    
    public DoubleMagicEventMapper()
    {
        WillSerialize("double-magic-event-v3");
        CanDeserialize("double-magic-event-v3");
        
        // V1
        CanDeserialize("double-magic-event", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<DoubleMagicEventV1>(data, options)!;
            return new DoubleMagicEvent(magicEvent.Id, string.Empty, magicEvent.Created);
        });

        // V1
        CanDeserialize("double-magic-event-v2", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<DoubleMagicEventV2>(data, options)!;
            return new DoubleMagicEvent(magicEvent.Id, magicEvent.MagicSpell, magicEvent.Created);
        });
        
    }
}