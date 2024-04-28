using EventSourcing.FunctionTests.DI.InvalidAssembly.Events;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.DI.InvalidAssembly.Mappers;

public class InvalidSecondCustomEventMapper : AbstractEventMapper<CustomEvent>
{
    public InvalidSecondCustomEventMapper()
    {
        WillSerialize("my-custom-event");
        CanDeserialize("my-custom-event");
    }
}