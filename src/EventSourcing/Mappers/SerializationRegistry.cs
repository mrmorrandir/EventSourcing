using System.Diagnostics.CodeAnalysis;
using FluentResults;

namespace EventSourcing.Mappers;

/// <summary>
/// SerializationRegistry is a registry for serializing and deserializing events related to a specific aggregate type.
/// </summary>
/// <remarks>
/// <para>
/// The registry uses reflection to find all event mappers that implement <see cref="IEventMapper{TEvent}"/> (or better <see cref="AbstractEventMapper{TEvent}"/>) for the events of the specified aggregate type.
/// </para>
/// <para>
/// Since reflection is somewhat expensive, it is recommended to use the <c>EventSourcing.SourceGenerator</c> package to generate the mappers and the registry at compile time.
/// </para>
/// </remarks>
/// <typeparam name="TAggregate"></typeparam>
// The SerializationRegistry is a static class that uses reflection to find all event mappers that implement IEventMapper<T> for the events of the specified aggregate type.
// Therefore, the static fields should only be shared across all instances of the same aggregate type.
// Therefore, The StaticMemberInGenericType warning is suppressed to avoid warnings about static members in generic types.
[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public class SerializationRegistry<TAggregate> : ISerializationRegistry<TAggregate> where TAggregate : IAggregate
{
    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers;
    private static readonly Dictionary<Type, Func<IEvent, ISerializedEvent>> _serializers;
    private static readonly StateSerializer<TAggregate> _stateSerializer;
    static SerializationRegistry()
    {
        // Use reflection to find the event types that are concerned with this aggregate
        var aggregateType = typeof(TAggregate);
        var createMethods = aggregateType
            .GetMethods()
            .Where(m => m is { IsPublic: true, IsStatic: true, Name: "Create" } && m.GetParameters().Length == 1 && m.ReturnType == aggregateType)
            .ToList();
        var applyMethods = aggregateType
            .GetMethods()
            .Where(m => m is { IsPublic: true, IsStatic: false, Name: "Apply" } && m.GetParameters().Length == 1 && m.ReturnType == aggregateType)
            .ToList();
        var eventMethods = createMethods.Concat(applyMethods).ToList();
        var eventTypes = eventMethods.Select(m => m.GetParameters()[0].ParameterType).Distinct().ToList();
        
        // Find all event mappers that implement IEventMapper<T> with T = one of the eventTypes 
        var eventMappers = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>) && eventTypes.Contains(i.GenericTypeArguments[0])))
            .Select(t => (IEventMapper)Activator.CreateInstance(t)!)
            .ToList();
        
        // Create serializers and deserializers for each event mapper
        _serializers = eventMappers
            .ToDictionary(
                eventMapper => eventMapper.EventType,
                eventMapper =>
                {
                    var serializeMethod = eventMapper.GetType().GetMethod("Serialize");
                    var serializeDelegate = (Func<IEvent, ISerializedEvent>)(@event => (ISerializedEvent)serializeMethod!.Invoke(eventMapper, [@event])!);
                    return serializeDelegate;
                });
        _deserializers = eventMappers
            .SelectMany(em => em.Schemas.Select(t => new { Schema = t, Mapper = em }))
            .ToDictionary(
                schemaAndMapper => schemaAndMapper.Schema,
                schemaAndMapper =>
                {
                    var deserializeMethod = schemaAndMapper.Mapper.GetType().GetMethod("Deserialize")!;
                    var deserializeDelegate = (Func<string, string, IEvent>)((type, data) => (IEvent)deserializeMethod!.Invoke(schemaAndMapper.Mapper, [type, data])!);
                    return deserializeDelegate;
                });
        // Create a state serializer for the aggregate type
        _stateSerializer = new StateSerializer<TAggregate>();
    }
    
    public Result<ISerializedState> Serialize(TAggregate state)
    {
        return Result.Try(() => _stateSerializer.Serialize(state));
    }

    public Result<ISerializedEvent> Serialize(IEvent @event)
    {
        if (!_serializers.TryGetValue(@event.GetType(), out var serializer))
            return Result.Fail($"No serializer found for type {@event.GetType().Name}");

        return Result.Try(() => serializer(@event));
    }

    public Result<IEvent> Deserialize(string schema, string data)
    {
        if (!_deserializers.TryGetValue(schema, out var deserializer))
            return Result.Fail($"No deserializer found for type {schema}");

        return Result.Try(() => deserializer(schema, data));
    }
}