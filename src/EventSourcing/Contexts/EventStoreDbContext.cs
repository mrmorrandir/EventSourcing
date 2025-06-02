using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Contexts;

public class EventStoreDbContext : DbContext, IEventStoreDbContext
{
    public DbSet<EventEntity> Events { get; set; }
    
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options) 
    {
        
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

        base.OnModelCreating(modelBuilder);
    }
}