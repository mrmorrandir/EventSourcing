using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventSourcing.Contexts;

public interface IEventStoreDbContext
{
    DbSet<EventEntity> Events { get; set; }
    DbSet<StateEntity> States { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}