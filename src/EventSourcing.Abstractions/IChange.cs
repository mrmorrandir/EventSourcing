namespace EventSourcing.Abstractions;

public interface IChange
{
    /// <summary>The version that is expected to evolve from this change</summary>
    int ExpectedVersion { get; init; }

    /// <summary>The Event that changed the aggregate to the expected version</summary>
    IEvent Event { get; init; }
}