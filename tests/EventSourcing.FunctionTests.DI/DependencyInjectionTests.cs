using System.Reflection;
using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Publishers.RabbitMQPublisher;
using EventSourcing.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.FunctionTests.DI;

public class DependencyInjectionTests
{
    [Fact]
    public void RegistrationShouldWork_WhenEventsAndMappersAndProjectionsAreCorrect()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMappers(typeof(ValidAssembly.Events.CustomEvent).Assembly));
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(typeof(ValidAssembly.Events.CustomEvent).Assembly));
        });
        var provider = services.BuildServiceProvider();

        var eventMappers = provider.GetRequiredService<IEnumerable<IEventMapper>>().ToArray();
        var projections = provider.GetRequiredService<IEnumerable<IEventHandler>>().Select(s => (IEventHandler)s).ToList();
        var eventRepository = provider.GetRequiredService<IEventRepository>();
        
        eventMappers.Should().ContainSingle(m => m.Types.Contains("my-custom-event-v1") && m.EventType == typeof(ValidAssembly.Events.CustomEvent));
        eventMappers.Should().ContainSingle(m => m.Types.Contains("default-event-v1") && m.EventType == typeof(ValidAssembly.Events.DefaultEvent));
        eventMappers.Should().HaveCount(2);
        
        projections.Should().Contain(p => p.GetType() == typeof(ValidAssembly.Projections.CustomEventProjection));
        projections.Should().ContainSingle(p => p.GetType() == typeof(ValidAssembly.Projections.DefaultEventProjection));
        projections.Should().HaveCount(3);
    }
    
    [Fact]
    public void RegistrationShouldThrowException_WhenMultipleEventMappersExist()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcing(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMappers(typeof(InvalidAssembly.Events.CustomEvent).Assembly));
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(Assembly.GetExecutingAssembly()));
        });

        func.Should().Throw<InvalidOperationException>().WithMessage("*multiple*");
    }
    
    [Fact]
    public void RegistrationShouldThrowException_WhenNoEventMappersExist()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcing(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMappers(typeof(InvalidAssembly2.Events.DefaultEvent).Assembly, false));
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(Assembly.GetExecutingAssembly()).IgnoreUncoveredEvents());
        });

        func.Should().Throw<InvalidOperationException>().WithMessage("*uncovered events (in mappings)*");
    }
    
    [Fact]
    public void RegistrationShouldThrowException_WhenMissingMappersAreIgnored()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcing(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMappers(typeof(InvalidAssembly2.Events.DefaultEvent).Assembly, false).IgnoreUncoveredEvents());
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(Assembly.GetExecutingAssembly()).IgnoreUncoveredEvents());
        });

        func.Should().NotThrow();
    }
    
    [Fact]
    public void RegistrationShouldThrowException_WhenNoProjectionExists()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcing(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMapper<DefaultEventMapper<InvalidAssembly.Events.DefaultEvent>>());
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(Assembly.GetExecutingAssembly()));
        });

        func.Should().Throw<InvalidOperationException>().WithMessage("*uncovered events (in projections)*");
    }
    
    [Fact]
    public void RegistrationShouldWork_WhenMissingProjectionsAreIgnored()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcing(options =>
        {
            options.ConfigureEventStoreDbContext(dbOptions => dbOptions.UseInMemoryDatabase("Test"));
            options.ConfigureMapping(mappingOptions => mappingOptions.AddMapper<DefaultEventMapper<InvalidAssembly.Events.DefaultEvent>>());
            options.ConfigureProjections(projectionOptions => projectionOptions.AddProjections(typeof(InvalidAssembly.Events.DefaultEvent).Assembly).IgnoreUncoveredEvents());
        });

        func.Should().NotThrow();
    }
}