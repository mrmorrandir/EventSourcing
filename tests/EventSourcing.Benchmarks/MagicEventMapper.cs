using System.Text.Json;
using EventSourcing.Abstractions;
using EventSourcing.Mappers;

namespace EventSourcing.Benchmarks;

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

public record NonMagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;

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


public record DoubleMagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;

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