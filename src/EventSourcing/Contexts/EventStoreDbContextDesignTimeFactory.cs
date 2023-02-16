using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventSourcing.Contexts;

// The CoffeeLifeDbContextDesignTimeFactory class is used to create an instance of the CoffeeLifeDbContext class at design time.
// This class is used by the EF Core tools to create migrations.
// The class uses a MySql database.
public class EventStoreDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
    private const string _connectionString = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=Processes;Integrated Security=True;TrustServerCertificate=true;";

    public EventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlServer(_connectionString);
        return new EventStoreDbContext(optionsBuilder.Options);
    }
}