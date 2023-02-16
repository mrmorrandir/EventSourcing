using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Contexts;

public class EventStoreDbContext : DbContext, IEventStoreDbContext
{
    public DbSet<EventData> Events { get; set; }
    
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options) 
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventData>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EventData>()
            .HasIndex(e => e.StreamId);
        modelBuilder.Entity<EventData>()
            .HasIndex(e => new { e.StreamId, e.Version }).IsUnique();
        modelBuilder.Entity<EventData>()
            .Property(e => e.Data);
        modelBuilder.Entity<EventData>()
            .HasIndex(e => e.Created);

        base.OnModelCreating(modelBuilder);
    }
}