using EventSourcing.SourceGenerators.Target.Aggregates;
using EventSourcing.SourceGenerators.Target.Repositories;

namespace EventSourcing.SourceGenerators.Target;

public class Program
{
    public static void Main()
    {
        var id = Guid.NewGuid();
        var createEvent = new CreatedEvent(id, "Test-Name", "Test-Description", DateTimeOffset.Now);
        var changedEvent = new ChangedNameEvent(id, "Magic", DateTimeOffset.Now.AddMinutes(1));
        var changedEvent2 = new ChangedNameEvent(id, "SomeMagic", DateTimeOffset.Now.AddMinutes(2));
        var changedEvent3 = new ChangedDescriptionEvent(id, "What is this here?", DateTimeOffset.Now.AddMinutes(3));
        
        var myEvents = new List<IEvent>
        {
            createEvent,
            changedEvent,
            changedEvent2,
            changedEvent3
        };

        Console.WriteLine("Working with events:");
        Console.WriteLine();
        var repository = new MyTestAggregateRepository();
        MyTestAggregate? state = null;
        foreach (var evt in myEvents)
        {
            Console.WriteLine($"Event: {evt.GetType().Name}, Data: {evt}");
            state = repository.SaveAndGet(id, [evt]);
            Console.WriteLine($"State: {state}");
            Console.WriteLine();
        }
        
        Console.WriteLine($"Final\nState: {state}");

        Console.WriteLine();
        // Serialize the state to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        Console.WriteLine("Serialized State:");
        Console.WriteLine(json);
    }
}