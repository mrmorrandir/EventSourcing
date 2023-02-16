using System.Collections.Concurrent;
using EventSourcing.Abstractions;
using Microsoft.Extensions.Logging;

namespace EventSourcing;

/// <summary>
/// This class is used to publish events to the event bus.
/// <para>
/// The event bus is a mediator that is used to publish events to the event handlers.
/// It is inspired by the MediatR library.
/// (Some parts of the code are copied from the MediatR library.)
/// </para>
/// </summary>
public class EventBus : IEventBus
{
    private static readonly ConcurrentDictionary<Type, EventHandlerWrapper> _eventHandlerWrappers = new();
    
    private readonly ILogger<EventBus>? _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus>? logger = null)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _logger?.LogTrace("Event bus created.");
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        
        return PublishEventAsync(@event, cancellationToken);
    }

    public Task PublishAsync(object notification, CancellationToken cancellationToken = default)
    {
        return notification switch
        {
            null => throw new ArgumentNullException(nameof(notification)),
            IEvent @event => PublishEventAsync(@event, cancellationToken),
            _ => throw new ArgumentException($"Event must implement {nameof(IEvent)}", nameof(notification))
        };
    }

    private Task PublishEventAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();
        var eventHandlerWrapper = _eventHandlerWrappers.GetOrAdd(eventType,
            static TArgument => 
                (EventHandlerWrapper)(Activator.CreateInstance(typeof(EventHandlerWrapper<>).MakeGenericType(TArgument))
                                      ?? throw new InvalidOperationException($"Could not create EventHandlerWrapper for type {TArgument}")));
        return eventHandlerWrapper.Handle(@event, _serviceProvider, PublishCoreAsync, cancellationToken);
    }

    private async Task PublishCoreAsync(IEnumerable<Func<IEvent, CancellationToken, Task>> handlerFunctions, IEvent @event, CancellationToken cancellationToken = default)
    {
        foreach (var handlerFunction in handlerFunctions)
        {
            try
            {
                _logger?.LogTrace("Publishing event {@Event} to handler {Handler}.", @event, handlerFunction.Method);
                await handlerFunction(@event, cancellationToken).ConfigureAwait(false);
                _logger?.LogTrace("Event {@Event} published to handler {Handler}.", @event, handlerFunction.Method);
                
            }
            catch (Exception ex)
            {
                // TODO: Add some message broker mechanism to the event bus to publish the exception.
                _logger?.LogError(ex, "Error while publishing event {Event}", @event);
            }
        }
    }
}

