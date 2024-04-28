using EventSourcing.FunctionTests.DI.InvalidAssembly.Events;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.DI.InvalidAssembly.Mappers;

public class CustomEventMapper : AbstractEventMapper<CustomEvent>
{
    public CustomEventMapper()
    {
        WillSerialize("my-custom-event");
        CanDeserialize("my-custom-event");
    }
}