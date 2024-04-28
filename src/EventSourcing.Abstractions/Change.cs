namespace EventSourcing;

/// <summary>
/// Documents a Change to an Aggregate
/// </summary>
/// <param name="ExpectedVersion">The version that is expected to evolve from this change</param>
/// <param name="Event">The Event that changed the aggregate to the expected version</param>
public record Change(int ExpectedVersion, IEvent Event) : IChange;