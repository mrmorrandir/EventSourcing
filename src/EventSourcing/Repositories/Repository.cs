using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.Repositories;

/// <summary>
/// Represents a generic repository for aggregates, providing methods to create and update aggregate roots using event sourcing patterns.
/// This repository coordinates event storage, serialization, aggregation, and projection to ensure consistency and traceability of aggregate state changes.
///
/// The repository requires the following services:
/// <list type="bullet">
///   <item>
///     <description><see cref="IEventStore"/>: Responsible for persisting and retrieving event streams associated with aggregates.</description>
///   </item>
///   <item>
///     <description><see cref="ISerializationRegistry{TAggregate}"/>: Handles serialization and deserialization of events to and from a storable format.</description>
///   </item>
///   <item>
///     <description><see cref="IAggregator{TAggregate}"/>: Reconstructs aggregate instances from event streams and applies new events to existing aggregates.</description>
///   </item>
///   <item>
///     <description><see cref="IEnumerable{IProjector{TAggregate}}"/>: Executes projections for each event, enabling read model updates or side effects.</description>
///   </item>
/// </list>
///
/// The repository ensures that all operations are transactional and that errors in any step (event creation, serialization, appending, projection, or saving) are reported with detailed error information. It is designed to be used in event-sourced systems where aggregate state is derived from a sequence of events.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate root managed by this repository. Must implement <see cref="IAggregate"/>.</typeparam>
public class Repository<TAggregate> : IRepository<TAggregate> where TAggregate : IAggregate
{
    private readonly IEventStore _eventStore;
    private readonly ISerializationRegistry<TAggregate> _serializationRegistry;
    private readonly IAggregator<TAggregate> _aggregator;
    private readonly IEnumerable<IProjector<TAggregate>> _projectors;

    public Repository(IEventStore eventStore, ISerializationRegistry<TAggregate> serializationRegistry, IAggregator<TAggregate> aggregator, IEnumerable<IProjector<TAggregate>> projectors)
    {
        _eventStore = eventStore;
        _serializationRegistry = serializationRegistry;
        _aggregator = aggregator;
        _projectors = projectors;
    }

    /// <summary>
    ///     Creates a new aggregate by executing the provided creation function (<paramref name="create"/> and saving the resulting event.
    ///     <para>
    ///     The creation function should return an <see cref="IEvent"/> representing the creation of the aggregate.
    ///     </para>
    ///     <para>
    ///     If the creation fails or an error occurs during event serialization, appending, or saving, a failed result is returned with error details.
    ///     </para>
    /// </summary>
    /// <param name="create">A function that returns an event for the creation of an aggregate (passed to the corresponding `Create` method of the aggregate).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A result containing the created aggregate instance on success, or error details on failure.</returns>
    public async Task<Result<TAggregate>> CreateAsync<TEvent>(Func<Task<TEvent>> create, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        var createResult = await Result.Try(create);
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);

        return await CreateInternalAsync(createResult.Value, cancellationToken);
    }
    
    /// <inheritdoc cref="CreateAsync{TEvent}(Func{Task{TEvent}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> CreateAsync<TEvent>(Func<TEvent> create, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        return await CreateAsync(() => Task.FromResult(create()), cancellationToken);
    }
    /// <inheritdoc cref="CreateAsync{TEvent}(Func{Task{TEvent}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> CreateAsync<TEvent>(Func<Task<Result<TEvent>>> create, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        var createResult = await create();
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);
        
        return await CreateInternalAsync(createResult.Value, cancellationToken);
    }
    
    /// <inheritdoc cref="CreateAsync{TEvent}(Func{Task{TEvent}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> CreateAsync<TEvent>(Func<Result<TEvent>> create, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        return await CreateAsync(() => Task.FromResult(create()), cancellationToken);
    }
    

    /// <summary>
    /// <para>
    /// Updates an existing aggregate by loading the event stream, applying the existing events and providing the aggregate to the update function (given by <paramref name="update"/>).
    /// </para>
    /// <para>
    /// When an error occurs in the <paramref name="update"/> function, the method will return a failed result with the error details.
    /// </para>
    /// <para>
    /// During the update process, the new events are applied to the aggregate temporary, and projections are executed for each new event.
    /// When there are errors during the projections (<see cref="IProjector{TAggregate}"/>), the method will return a failed result with the error details. No events will be saved in this case, and the aggregate will not be updated.
    /// </para>
    /// </summary>
    /// <param name="aggregateId">The id of the aggregate (aka event stream id)</param>
    /// <param name="update">A function to provide events as update to the aggregate.</param>
    /// <param name="cancellationToken">The cancellationToken parameter allows the asynchronous operation to be canceled</param>
    /// <returns>A result indicating success or error of the operation. On success the updated aggregate instance is provided.</returns>
    /// <example>
    /// <code language="csharp">
    /// var repository = new Repository&lt;MyAggregate&gt;(eventStore, serializationRegistry, aggregator, projectors);
    /// var result = await repository.UpdateAsync(
    ///     aggregateId,
    ///     async aggregate =&gt;
    ///     {
    ///         // Apply some business logic and return new events
    ///         var newEvent = new MyEvent { /* ... */ };
    ///         return new List&lt;IEvent&gt; { newEvent };
    ///     },
    ///     cancellationToken);
    /// if (result.IsFailed)
    /// {
    ///     // Handle errors
    ///     return;
    /// }
    /// var updatedAggregate = result.Value;
    /// // work with the updated aggregate
    /// </code>
    /// </example>
    public async Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Task<List<IEvent>>> update, CancellationToken cancellationToken = default)
    {
        return await UpdateInternalAsync(aggregateId, UpdateMethod , cancellationToken);

        Task<Result<List<IEvent>>> UpdateMethod(TAggregate aggregate) => Result.Try(() => update(aggregate));
    }
  
    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, List<IEvent>> update, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(aggregateId, aggregate => Task.FromResult(update(aggregate)), cancellationToken);
    }

    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync<TEvent>(Guid aggregateId, Func<TAggregate, Task<TEvent>> update, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        return await UpdateAsync(aggregateId, async aggregate => [await update(aggregate)], cancellationToken);
    }
    
    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync<TEvent>(Guid aggregateId, Func<TAggregate, TEvent> update, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        return await UpdateAsync(aggregateId, aggregate => Task.FromResult<List<IEvent>>([update(aggregate)]), cancellationToken);
    }
    
    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Task<Result<List<IEvent>>>> update, CancellationToken cancellationToken = default)
    {
        return await UpdateInternalAsync(aggregateId, update , cancellationToken);
    }
  
    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Result<List<IEvent>>> update, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(aggregateId, aggregate => Task.FromResult(update(aggregate)), cancellationToken);
    }

    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync<TEvent>(Guid aggregateId, Func<TAggregate, Task<Result<TEvent>>> update, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        return await UpdateInternalAsync(aggregateId, async aggregate =>
        {
            var result = await update(aggregate);
            if (result.IsFailed)
                return Result.Fail(result.Errors);
            return Result.Ok(new List<IEvent> { result.Value });
        }, cancellationToken);
        
    }
    
    /// <inheritdoc cref="UpdateAsync(Guid, Func{TAggregate, Task{List{IEvent}}}, CancellationToken)"/>
    public async Task<Result<TAggregate>> UpdateAsync<TEvent>(Guid aggregateId, Func<TAggregate, Result<TEvent>> update, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        return await UpdateInternalAsync(aggregateId, aggregate =>
        {
            var result = update(aggregate);
            if (result.IsFailed)
                return Task.FromResult<Result<List<IEvent>>>(Result.Fail(result.Errors));
            return Task.FromResult(Result.Ok(new List<IEvent> { result.Value }));
        }, cancellationToken);
    }
    
    private async Task<Result<TAggregate>> CreateInternalAsync<TEvent>(TEvent createdEvent, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        var eventStreamResult = await _eventStore.CreateStreamAsync(createdEvent.AggregateId, cancellationToken);
        if (eventStreamResult.IsFailed)
            return new Error("Failed to create event stream").CausedBy(eventStreamResult.Errors);
        
        var eventStream = eventStreamResult.Value;
        
        var serializeResult = _serializationRegistry.Serialize(createdEvent);
        if (serializeResult.IsFailed)
            return new Error($"Failed to serialize event of type {createdEvent.GetType().Name}").CausedBy(serializeResult.Errors);
        
        var serializedEvent = serializeResult.Value;
        var eventEntity = new EventEntity(createdEvent.AggregateId, 1, serializedEvent.Schema, serializedEvent.Data);
        
        var appendResult = await eventStream.AppendAsync(eventEntity, cancellationToken);
        if (appendResult.IsFailed)
            return new Error("Failed to append event to stream").CausedBy(appendResult.Errors);
        
        var createAggregateResult = CreateAggregateFromEvents([createdEvent]);
        if (createAggregateResult.IsFailed)
            return new Error("Failed to create aggregate from events").CausedBy(createAggregateResult.Errors);
        
        // Project the new events
        foreach (var projector in _projectors)
        {
            var projectionResult = await projector.ProjectAsync(createAggregateResult.Value, createdEvent, cancellationToken);
            if (projectionResult.IsFailed)
                return new Error($"Failed to project event of type {createdEvent.GetType().Name}").CausedBy(projectionResult.Errors);
        }

        // Save the event stream
        var saveResult = await eventStream.SaveAsync(cancellationToken);
        if (saveResult.IsFailed)
            return new Error("Failed to save event stream").CausedBy(saveResult.Errors);
        
        return createAggregateResult.Value;
    }
    
      private async Task<Result<TAggregate>> UpdateInternalAsync(Guid aggregateId, Func<TAggregate, Task<Result<List<IEvent>>>> update, CancellationToken cancellationToken)
    {
        // Get the event stream for the aggregate
        var getResult = await _eventStore.GetStreamAsync(aggregateId, cancellationToken);
        if (getResult.IsFailed)
            return new Error("Failed to get event stream").CausedBy(getResult.Errors);
        
        var eventStream = getResult.Value;
        
        var originalEventsResult = CreateEventsFromEntities(eventStream.Events);
        if (originalEventsResult.IsFailed)
            return new Error("Failed to create events from event entities").CausedBy(originalEventsResult.Errors);
        
        // Create the aggregate from the events in the stream
        var originalAggregateResult = CreateAggregateFromEvents(originalEventsResult.Value);
        if (originalAggregateResult.IsFailed)
            return originalAggregateResult.ToResult();
        
        var originalAggregate = originalAggregateResult.Value;
        
        // Execute the update function to get the new events
        var updateFunctionResult = await update(originalAggregate);
        if (updateFunctionResult.IsFailed)
            return new Error("Failed to update aggregate").CausedBy(updateFunctionResult.Errors);

        // Check if there are any new events to append
        var newEvents = updateFunctionResult.Value.ToList();
        if (newEvents.Count == 0)
            return Result.Ok(originalAggregate).WithSuccess("No changes made to the aggregate, nothing to save.");

        // Append the new events to the event stream
        var expectedVersion = eventStream.BaseVersion;
        foreach (var newEvent in newEvents)
        {
            var newSerializedEventResult = _serializationRegistry.Serialize(newEvent);
            if (newSerializedEventResult.IsFailed)
                return new Error($"Failed to serialize event of type {newEvent.GetType().Name}").CausedBy(newSerializedEventResult.Errors);

            var newSerializedEvent = newSerializedEventResult.Value;
            var newEventEntity = new EventEntity(newEvent.AggregateId, ++expectedVersion, newSerializedEvent.Schema, newSerializedEvent.Data);
            var appendResult = await eventStream.AppendAsync(newEventEntity, cancellationToken);
            if (appendResult.IsFailed)
                return new Error("Failed to append event to stream").CausedBy(appendResult.Errors);
        }
        
        var updatedAggregateResult = CreateAggregateFromEvents(originalEventsResult.Value.Concat(newEvents));
        if (updatedAggregateResult.IsFailed)
            return new Error("Failed to update aggregate from events").CausedBy(updatedAggregateResult.Errors);
        
        var updatedAggregate = updatedAggregateResult.Value;
        // Project the new events
        foreach (var projector in _projectors)
        {
            foreach (var evt in newEvents)
            {
                var projectionResult = await projector.ProjectAsync(updatedAggregate, evt, cancellationToken);
                if (projectionResult.IsFailed)
                    return new Error($"Failed to project event of type {evt.GetType().Name}").CausedBy(projectionResult.Errors);
            }
        }

        // Save the event stream
        var saveResult = await eventStream.SaveAsync(cancellationToken);
        if (saveResult.IsFailed)
            return new Error("Failed to save event stream").CausedBy(saveResult.Errors);

        return Result.Ok(updatedAggregate);
    }


    private Result<List<IEvent>> CreateEventsFromEntities(IEnumerable<EventEntity> eventEntities)
    {
        var events = new List<IEvent>();
        foreach (var eventEntity in eventEntities)
        {
            var deserializeResult = _serializationRegistry.Deserialize(eventEntity.Schema, eventEntity.Data);
            if (deserializeResult.IsFailed)
                return new Error("Failed to deserialize event").CausedBy(deserializeResult.Errors);

            var @event = deserializeResult.Value;
            events.Add(@event);
        }

        return events;
    }
    
    private Result<TAggregate> CreateAggregateFromEvents(IEnumerable<IEvent> events)
    {
        TAggregate? aggregate = default;
        foreach (var @event in events)
        {
            if (aggregate == null)
            {
                var createResult = _aggregator.CreateFromEvent(@event);
                if (createResult.IsFailed)
                    return new Error($"Failed to create aggregate from event of type {@event.GetType().Name}").CausedBy(createResult.Errors);
                aggregate = createResult.Value;
                continue;
            }
            var applyResult =_aggregator.ApplyEvent(aggregate, @event);
            if (applyResult.IsFailed)
                return new Error($"Failed to apply event of type {@event.GetType().Name} to aggregate").CausedBy(applyResult.Errors);
            aggregate = applyResult.Value;
        }

        return aggregate == null ? new Error("Aggregate could not be created from events") : Result.Ok(aggregate);
    }
}