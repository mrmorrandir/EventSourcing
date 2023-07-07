using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.DI.InvalidAssembly;

public class InvalidSecondCustomEventMapper : AbstractEventMapper<CustomEvent>
{
    public InvalidSecondCustomEventMapper()
    {
        WillSerialize("my-custom-event");
        CanDeserialize("my-custom-event");
    }
}