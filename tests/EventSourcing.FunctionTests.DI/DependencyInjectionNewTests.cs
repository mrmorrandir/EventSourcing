using System.Reflection;
using EventSourcing.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.FunctionTests.DI;

public class DependencyInjectionNewTests
{
    [Fact]
    public void RegistrationShouldWork_WhenEventMappersAndEventsAreCorrect()
    {
        var services = new ServiceCollection();
        services.AddEventSourcingNew(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMappers(typeof(ValidAssembly.CustomEvent).Assembly));
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(Assembly.GetExecutingAssembly()));
        });
        var provider = services.BuildServiceProvider();

        var eventMappers = provider.GetRequiredService<IEnumerable<IEventMapper>>().ToArray();
        
        eventMappers.Should().ContainSingle(m => m.Types.Contains("my-custom-event-v1") && m.EventType == typeof(ValidAssembly.CustomEvent));
        eventMappers.Should().ContainSingle(m => m.Types.Contains("default-event-v1") && m.EventType == typeof(ValidAssembly.DefaultEvent));
        eventMappers.Should().HaveCount(2);
    }
    
    [Fact]
    public void RegistrationShouldThrowException_WhenMultipleEventMappersExist()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcingNew(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMappers(typeof(InvalidAssembly.CustomEvent).Assembly));
        });

        func.Should().Throw<InvalidOperationException>().WithMessage("*multiple*");

    }
}