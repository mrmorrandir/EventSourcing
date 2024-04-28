using System.Text.Json;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers;

public class MagicEventMapper : AbstractEventMapper<MagicEvent>
{
    public record MagicEventV1(Guid Id, DateTime Created) : IEvent;
    public record MagicEventV2(Guid Id, string MagicSpell, DateTime Created) : IEvent;
    
    public MagicEventMapper()
    {
        WillSerialize("magic-event-v3");
        CanDeserialize("magic-event-v3");
        
        // V1
        CanDeserialize("magic-event", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<MagicEventV1>(data, options)!;
            return new MagicEvent(magicEvent.Id, string.Empty, magicEvent.Created);
        });

        // V1
        CanDeserialize("magic-event-v2", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<MagicEventV2>(data, options)!;
            return new MagicEvent(magicEvent.Id, magicEvent.MagicSpell, magicEvent.Created);
        });
        
    }
}