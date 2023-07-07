using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.DI.InvalidAssembly;

public class CustomEventMapper : AbstractEventMapper<CustomEvent>
{
    public CustomEventMapper()
    {
        WillSerialize("my-custom-event");
        CanDeserialize("my-custom-event");
    }
}