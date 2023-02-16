using EventSourcing.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing;

internal abstract class EventHandlerWrapper
{
    public abstract Task Handle(
        IEvent @event,
        IServiceProvider serviceProvider,
        Func<IEnumerable<Func<IEvent, CancellationToken, Task>>, IEvent, CancellationToken, Task> publish,
        CancellationToken cancellationToken = default);
}

internal class EventHandlerWrapper<TEvent> : EventHandlerWrapper where TEvent : IEvent 
{
    public override Task Handle(
        IEvent @event, 
        IServiceProvider serviceProvider, 
        Func<IEnumerable<Func<IEvent, CancellationToken, Task>>, IEvent, CancellationToken, Task> publish,
        CancellationToken cancellationToken = default)
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();
        var handlerFunctions = handlers.Select(static handler =>
            new Func<IEvent, CancellationToken, Task>((theEvent, theToken) =>
                handler.HandleAsync((TEvent)theEvent, theToken)));

        return publish(handlerFunctions, @event, cancellationToken);
    }
}