using EventSourcing.Abstractions;
using EventSourcing.SourceGenerators.Target.Aggregates;
using EventSourcing.SourceGenerators.Target.Aggregates.Generated;
using EventSourcing.SourceGenerators.Target.Events;
using EventSourcing.SourceGenerators.Target.Repositories;

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
        
        Console.WriteLine();
        Console.WriteLine();

        var id = Guid.NewGuid();
        var createEvent = new CreatedEvent(id, "Test-Name", "Test-Description", DateTimeOffset.Now);
        var changedEvent = new ChangedNameEvent(id, "Magic", DateTimeOffset.Now.AddMinutes(1));
        var changedEvent2 = new ChangedNameEvent(id, "SomeMagic", DateTimeOffset.Now.AddMinutes(2));
        var changedEvent3 = new ChangedDescriptionEvent(id, "What is this here?", DateTimeOffset.Now.AddMinutes(3));
        
        var myEvents = new List<IAggregateEvent>
        {
            createEvent,
            changedEvent,
            changedEvent2,
            changedEvent3
        };

        MyTestAggregate? state = null;
        var version = 0;
        foreach (var @event in myEvents)
        {
            if (++version == 1)
            {
                state = MyTestAggregateDispatcher.CreateFromEvent(@event);
                Console.WriteLine($"Created state from event: {state}");
                continue;
            }
            state = MyTestAggregateDispatcher.ApplyEvent(state, @event);
            Console.WriteLine($"Applied event: {@event.GetType().Name} with data {@event} => {state}");
        }
        
        Console.WriteLine($"State (CurrentVersion: {version}): {state}");
        Console.WriteLine();
        
        var repository = new MyTestAggregateRepository();
        var state2 = repository.SaveAndGet(id, myEvents);
        
        Console.WriteLine($"State from repository: {state2}");
    }
}