using EventSourcing.Contexts;
using EventSourcing.Stores;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// Don't know if I will need this.
/// </summary>
public class EventStreamFactory : IEventStreamFactory
{
    private readonly IEventStoreDbContext _context;

    public EventStreamFactory(IEventStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEventStream> CreateAsync(Guid streamId, List<EventEntity> events, CancellationToken cancellationToken = default)
    {
        return new EventStream(_context, streamId, events);
    }
}