using EventSourcing.Abstractions;

namespace EventSourcing.Benchmarks;

public record TestCreated(Guid Id, string Name, DateTime Created) : IEvent;
public record TestChanged(Guid Id, string Name, DateTime Changed) : IEvent;

public class TestAggregate : AggregateRoot
{
    public string Name { get; private set; }
    
    private TestAggregate(){}

    public static TestAggregate Create(string name)
    {
        var aggregate = new TestAggregate();
        aggregate.ApplyChange(new TestCreated(Guid.NewGuid(), name, DateTime.Now));
        return aggregate;
    }
    
    public void Change(string name)
    {
        ApplyChange(new TestChanged(Id, name, DateTime.Now));
    }
    
    protected override void Apply(IEvent @event)
    { 
        switch (@event)
        {
            case TestCreated e:
                Id = e.Id;
                Name = e.Name;
                break;
            case TestChanged e:
                Name = e.Name;
                break;
        }
    }
}