using System.Reflection;
using EventSourcing.Abstractions;
using EventSourcing.Abstractions.Mappers;
using EventSourcing.Abstractions.Repositories;
using EventSourcing.Abstractions.Stores;
using EventSourcing.Contexts;
using EventSourcing.FunctionTests.DependencyInjections.Events;
using EventSourcing.FunctionTests.DependencyInjections.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.FunctionTests.DependencyInjections;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(typeof(IEventStoreDbContext))]
    [InlineData(typeof(IEventStore))]
    [InlineData(typeof(IEventRepository))]
    [InlineData(typeof(IEventMapper))]
    [InlineData(typeof(IEventBus))]
    public void CanResolveTypes(Type type)
    {
        var provider = SetupDependencyInjection();
        var resolvedType = provider.GetService(type);
        resolvedType.Should().NotBeNull();
    }
    
    [Fact]
    public void CanFindAllProjections()
    {
        var provider = SetupDependencyInjection();
        var projections1 = provider.GetServices<IEventHandler<TestEvent1>>();
        projections1.Should().NotBeEmpty();
        projections1.Should().HaveCount(2);
        projections1.Should().Contain(p => p.GetType() == typeof(TestProjection1));
        projections1.Should().Contain(p => p.GetType() == typeof(TestProjection2));
        
        var projections2 = provider.GetServices<IEventHandler<TestEvent2>>();
        projections2.Should().NotBeEmpty();
        projections2.Should().HaveCount(1);
        projections2.Should().Contain(p => p.GetType() == typeof(TestProjection2));
        
    }

    [Fact]
    public void CanRegisterSimpleEventMap()
    {
        var provider = SetupDependencyInjection();
        provider.AddEventMappings(mapping =>
        {
            mapping.Register<TestEvent1>("test-event-1");
        });
        
        var mapper = provider.GetService<IEventMapper>();
        mapper.Should().NotBeNull();
        var mapForth = () => mapper.Map(new TestEvent1());
        var data = mapForth.Should().NotThrow().Subject;
        data.Should().NotBeNull();
        
        var mapBack = () => mapper.Map("test-event-1", data);
        var @event = mapBack.Should().NotThrow().Subject;
    }
    
    [Fact]
    public void CanRegisterCustomEventMap()
    {
        var provider = SetupDependencyInjection();
        provider.AddEventMappings(mapping =>
        {
            mapping.Register("test-event-1", new TestEventMap());
        });
        
        var mapper = provider.GetService<IEventMapper>();
        mapper.Should().NotBeNull();
        var mapForth = () => mapper.Map(new TestEvent1());
        var data = mapForth.Should().NotThrow().Subject;
        data.Should().NotBeNull();
        data.Should().Match("The data for *");
        
        var mapBack = () => mapper.Map("test-event-1", data);
        var @event = mapBack.Should().NotThrow().Subject;
    }

    private static ServiceProvider SetupDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(options => options.UseInMemoryDatabase("Test"));
        services.AddProjections(Assembly.GetExecutingAssembly());
        var provider = services.BuildServiceProvider();
        return provider;
    }
}