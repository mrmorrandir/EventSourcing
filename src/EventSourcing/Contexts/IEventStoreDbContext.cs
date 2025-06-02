using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Contexts;

public interface IEventStoreDbContext
{
    DbSet<EventEntity> Events { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}