using EventSourcing.Contexts;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate1;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public class ExampleUsage
{
    private readonly IMyAggregateRepository _repository;
    private readonly IEventStoreDbContext _context;
    private readonly MyTestAggregateRepositoryX _repositoryX;

    public ExampleUsage(IMyAggregateRepository repository, IEventStoreDbContext context, MyTestAggregateRepositoryX repositoryX)
    {
        _repository = repository;
        _context = context;
        _repositoryX = repositoryX;
    }

    public async Task Use()
    {
        var getResult = await _repository.GetAsync(Guid.Empty, CancellationToken.None);
        if (getResult.IsFailed)
        {
            Console.WriteLine($"Failed to get aggregate: {string.Join(", ", getResult.Errors.Select(e => e.Message))}");
            return;
        }
        var (myAggregate, version) = getResult.Value;

        var changedNameEvent = new ChangedNameEvent(myAggregate.Id, "Teufelnocheins", DateTimeOffset.Now);
        
        var updateResult = await _repository.SaveAndUpdateAsync(myAggregate.Id, [changedNameEvent], version, CancellationToken.None);
        if (updateResult.IsFailed)
        {
            Console.WriteLine($"Failed to update aggregate: {string.Join(", ", updateResult.Errors.Select(e => e.Message))}");
            return;
        }
        (myAggregate, version) = updateResult.Value;
        
        Console.WriteLine($"Updated aggregate (to version {version}): {myAggregate}");


        var updateResult2 = await _repository.UpdateAsync(Guid.Empty, aggregate => [new ChangedDescriptionEvent(aggregate.Id, "Neue Beschreibung", DateTimeOffset.Now)], CancellationToken.None);
        
        // New RepositoryX usage example
        _repositoryX.CreateAsync(() =>
        {
            return Task.FromResult(Result.Ok(new CreatedEvent(Guid.NewGuid(), "Test", "Test Description", DateTimeOffset.Now)));
        }, CancellationToken.None);

    }

    public async Task UseEventRepositoryX()
    {
        var eventStore = new EventStoreX(_context);
        var eventRepository = new MyTestAggregateRepositoryX(eventStore);

        var createdResult = await eventRepository.CreateAsync(() =>
        {
            return Task.FromResult<Result<CreatedEvent>>(new CreatedEvent(Guid.NewGuid(), "Test", "Test Description", DateTimeOffset.Now));
        }, CancellationToken.None);

        var updateResult = await eventRepository.UpdateAsync(Guid.Empty, async aggregate =>
        {
            //return Result.Fail("This is a failure message");
            return new List<IEvent> { new ChangedNameEvent(aggregate.Id, "New Name", DateTimeOffset.Now) };
        }, CancellationToken.None);
    }
}

public static class ResultExtensions {
    
    public static async Task<Result<TResult>> HandleResult<T, TResult>(this Task<Result<T>> task, Func<T, Task<TResult>> onSuccess, Func<T, Result>? onFailure = null)
    {
        var result = await task;
        if (result.IsFailed)
            return onFailure == null ? Result.Fail(result.Errors) : onFailure(result.Value);
        return await onSuccess(result.Value);
    } 
    
    public static async Task<Result<TResult>> OnSuccess<T, TResult>(this Task<Result<T>> task, Func<T, TResult> onSuccess)
    {
        var result = await task;
        if (result.IsFailed)
            return Result.Fail(result.Errors);
        return onSuccess(result.Value);
    }
    
    public static async Task<Result> OnSuccess<T>(this Task<Result<T>> task, Func<T, Result> onSuccess)
    {
        var result = await task;
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        return onSuccess(result.Value);
    }

    // TODO: Diese Methode kann theoretisch auch Teil des Repositories sein, da sie eine Update-Operation ist.
    public static async Task<Result<Aggregate<TAggregate>>> UpdateAsync<TAggregate>(this IAggregateRepository<TAggregate> repository, Guid id, Func<TAggregate, IEnumerable<IEvent>> update, CancellationToken cancellationToken) where TAggregate : IAggregate
    {
        var getResult = await repository.GetAsync(id, cancellationToken);
        if (getResult.IsFailed)
            return getResult;
        var (aggregate, version) = getResult.Value;
        var events = update(aggregate);
        return await repository.SaveAsync(id, events, version, cancellationToken);
    }
    
    public static async Task<Result<Aggregate<TAggregate>>> UpdateAsync<TAggregate>(this IAggregateRepository<TAggregate> repository, Guid id, Func<TAggregate, CancellationToken, Task<IEnumerable<IEvent>>> update, CancellationToken cancellationToken) where TAggregate : IAggregate
    {
        var getResult = await repository.GetAsync(id, cancellationToken);
        if (getResult.IsFailed)
            return getResult;
        var (aggregate, version) = getResult.Value;
        var events = await update(aggregate, cancellationToken);
        return await repository.SaveAsync(id, events, version, cancellationToken);
    }
}