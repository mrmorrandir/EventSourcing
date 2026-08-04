using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventSourcing.Contexts;

public class EventStoreDbContext : DbContext, IEventStoreDbContext
{
    public DbSet<EventEntity> Events { get; set; }
    
    public DbSet<StateEntity> States { get; set; }
    
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options) 
    {
        
    }
    
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.StreamId);
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => new { e.StreamId, e.Version }).IsUnique();
        modelBuilder.Entity<EventEntity>()
            .Property(e => e.Data);
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.Created);
        
        modelBuilder.Entity<StateEntity>()
            .HasKey(s => s.Id);

        base.OnModelCreating(modelBuilder);
    }
}