using System.Collections.Concurrent;
using EventSourcing.Abstractions;

namespace EventSourcing;

public sealed class Unsubscriber : IDisposable
{
    private readonly Type _type;
    private readonly ConcurrentDictionary<Type,ConcurrentBag<Func<IEvent, CancellationToken, Task>>> _handlers;
    private Func<IEvent, CancellationToken, Task> _handler;

    public Unsubscriber(Type type, ConcurrentDictionary<Type,ConcurrentBag<Func<IEvent, CancellationToken, Task>>> handlers, Func<IEvent, CancellationToken, Task> handler)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Dispose()
    {
        if (_handlers.TryGetValue(_type, out var handlers))
            handlers.TryTake(out _handler!);
    }
}