using System.Text.Json;
using EventSourcing.FunctionTests.Mappers.Events;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers.Mappers;

public class MagicEventMapper : AbstractEventMapper<MagicEvent>
{
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
        CanDeserialize("magic-event-v1", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<MagicEventV1>(data, options)!;
            return new MagicEvent(magicEvent.Id, string.Empty, magicEvent.Created);
        });

        // V2
        CanDeserialize("magic-event-v2", (data, options) =>
        {
            var magicEvent = JsonSerializer.Deserialize<MagicEventV2>(data, options)!;
            return new MagicEvent(magicEvent.Id, magicEvent.MagicSpell, magicEvent.Created);
        });
        
    }
}