using System.Reflection;
using EventSourcing.Abstractions.Mappers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public class EventMapperConfig
{
    internal List<Type> CustomMappersToRegister { get; } = new();
    internal List<Assembly> AssembliesToRegisterCustomMappers { get; }= new();
    internal List<Assembly> AssembliesToRegisterDefaultMappers { get; }= new();

    /// <inheritdoc cref="DependencyInjection.AddCustomEventMappersFromAssembly"/>
    public EventMapperConfig AddCustomMappers(Assembly assembly)
    {
        AssembliesToRegisterCustomMappers.Add(assembly);
        return this;
    }
    
    public EventMapperConfig AddCustomMapper<TEventMapper>() where TEventMapper : class, IEventMapper
    {
        CustomMappersToRegister.Add(typeof(TEventMapper));
        return this;
    }

    /// <inheritdoc cref="DependencyInjection.AddDefaultEventMappersFromAssembly"/>
    public EventMapperConfig AddDefaultMappers(Assembly assembly)
    {
        AssembliesToRegisterDefaultMappers.Add(assembly);
        return this;
    }
}