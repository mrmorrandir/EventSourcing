using System.Text.Json;

namespace EventSourcing.SourceGenerators.Target.Events;

public record MyTestEvent(string Value, DateTimeOffset Timestamp) : IEvent;

public partial class MyTestEventMapper
{
    private record MyTestEventV1(string Value);

    partial void Configure()
    {
        WillSerialize("what-the-fuck-v2", true);
        CanDeserialize("what-the-fuck-v2");
        CanDeserialize("my-test-event-v1", DeserializeV1, true);
    }

    private MyTestEvent DeserializeV1(string data, JsonSerializerOptions jsonSerializerOptions)
    {
        var v1 = JsonSerializer.Deserialize<MyTestEvent>(data, jsonSerializerOptions);
        return new MyTestEvent(v1?.Value ?? string.Empty, DateTimeOffset.MinValue);
    }
}