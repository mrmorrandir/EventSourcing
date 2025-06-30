using EventSourcing;
using EventSourcing.SourceGenerators.Target.Domain.Processes.Events;

public enum ProcessState
{
    Pending,
    Running,
    Completed,
    Cancelled
}

public enum ProcessResult
{
    Unknown,
    Success,
    Failure
}

public record Process<T>(Guid Id, string Name, string Description, T Data, ProcessState State, ProcessResult Result) : IAggregate
{
    public static Process<T> Create(CreatedEvent<T> evt)
    {
        return new Process<T>(Guid.NewGuid(), evt.Name, evt.Description, evt.Data, ProcessState.Pending, ProcessResult.Unknown);
    }
    
    public Process<T> Apply(StartedEvent evt)
    {
        return this with { State = ProcessState.Running, Result = ProcessResult.Unknown };
    }
    
    public Process<T> Apply(CompletedEvent evt)
    {
        return this with { State = ProcessState.Completed, Result = evt.Result };
    }
    
    public Process<T> Apply(CancelledEvent evt)
    {
        return this with { State = ProcessState.Cancelled, Result = ProcessResult.Unknown };
    }
}

public record LubricantData(string Type, double Amount, double Destination);

public record LubricantProcess(Guid Id, string Name, string Description, LubricantData Data, ProcessState State, ProcessResult Result) :Process<LubricantData>(Id, Name, Description, Data, State, Result);