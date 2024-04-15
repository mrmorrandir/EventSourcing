using System.Reflection;
using EventSourcing.FunctionTests.DI.ValidAssembly.Events;
using EventSourcing.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.FunctionTests.DI;

public class DependencyInjectionTests
{
    [Fact]
    public void RegistrationShouldWork_WhenEventMappersAndEventsAreCorrect()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(builder => builder.UseInMemoryDatabase("Test"))
            .AddEventMappers(config =>
            {
                var assembly = typeof(CustomEvent).Assembly;
                config
                    .AddCustomMappers(assembly)
                    .AddDefaultMappers(assembly);
            });
        services.AddProjections(config => config.AddProjections(Assembly.GetExecutingAssembly()));
        var provider = services.BuildServiceProvider();

        var eventMappers = provider.GetRequiredService<IEnumerable<IEventMapper>>().ToArray();
        
        eventMappers.Should().ContainSingle(m => m.Types.Contains("my-custom-event-v1") && m.EventType == typeof(CustomEvent));
        eventMappers.Should().ContainSingle(m => m.Types.Contains("default-event-v1") && m.EventType == typeof(DefaultEvent));
        eventMappers.Should().HaveCount(2);
    }
    
    [Fact]
    public void RegistrationShouldThrowException_WhenMultipleEventMappersExist()
    {
        var services = new ServiceCollection();
        
        var func = () => services.AddEventSourcing(builder => builder.UseInMemoryDatabase("Test"))
            .AddEventMappers(config =>
            {
                var assembly = typeof(InvalidAssembly.Events.CustomEvent).Assembly;
                config
                    .AddCustomMappers(assembly)
                    .AddDefaultMappers(assembly);
            });

        func.Should().Throw<InvalidOperationException>().WithMessage("*multiple*");
    }
}



