using EventSourcing.SourceGenerators.Target.Events;

namespace EventSourcing.SourceGenerators.Target;

public class Program
{
    public static void Main()
    {
        var mapper = new MyTestEventMapper();
        Console.WriteLine($"The event type {mapper.EventType.Name} supports deserialization from {string.Join(", ", mapper.Types)}");
        
        var event1 = new MyTestEvent("Hallo", DateTimeOffset.Now);
        var serializedEvent1 = mapper.Serialize(event1);
        Console.WriteLine($"Serialized event 1: Type {serializedEvent1.Type} => {serializedEvent1.Data}");
        
        var fromWhatTheFuckV2 = mapper.Deserialize(serializedEvent1.Type, serializedEvent1.Data);
        Console.WriteLine($"Deserialized from what-the-fuck-v2: Value {fromWhatTheFuckV2.Value}, Timestamp {fromWhatTheFuckV2.Timestamp}");
        
        
        var json = "{\"value\": \"Hallo Welt\"}";
        Console.WriteLine($"Prepared data for my-test-event-v1: {json}");
        var fromMyTestEventV1 = mapper.Deserialize("my-test-event-v1", json);
        Console.WriteLine($"Deserialized from my-test-event-v1: Value {fromMyTestEventV1.Value}, Timestamp {fromMyTestEventV1.Timestamp}");
    }
}