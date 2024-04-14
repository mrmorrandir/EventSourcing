using EventSourcing.Contexts;
using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DPS2.Processes.Infrastructure.UnitTests.EventStores;

public class DatabaseEventStoreTests : EventStoreTests
{
    public override IEventStore EventStore {
        get
        {
            var services = new ServiceCollection();
            services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(options =>
                options.UseInMemoryDatabase("DPS2.EventStore"));
            services.AddScoped<IEventStore, EventStore>();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IEventStore>();
        }
    }
}