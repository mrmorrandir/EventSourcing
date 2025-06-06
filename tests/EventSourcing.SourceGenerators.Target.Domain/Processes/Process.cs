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

public record Process(Guid Id, string Name, string Description, ProcessState State, ProcessResult Result) : IAggregate
{
    public static Process Create(CreatedEvent evt)
    {
        return new Process(Guid.NewGuid(), evt.Name, evt.Description, ProcessState.Pending, ProcessResult.Unknown);
    }
    
    public Process Apply(StartedEvent evt)
    {
        return this with { State = ProcessState.Running, Result = ProcessResult.Unknown };
    }
    
    public Process Apply(CompletedEvent evt)
    {
        return this with { State = ProcessState.Completed, Result = evt.Result };
    }
    
    public Process Apply(CancelledEvent evt)
    {
        return this with { State = ProcessState.Cancelled, Result = ProcessResult.Unknown };
    }
}