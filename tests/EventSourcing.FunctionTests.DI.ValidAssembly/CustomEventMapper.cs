using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.DI.ValidAssembly;

public class CustomEventMapper : AbstractEventMapper<CustomEvent>
{
    public CustomEventMapper()
    {
        WillSerialize("my-custom-event-v1");
        CanDeserialize("my-custom-event-v1");
    }
}