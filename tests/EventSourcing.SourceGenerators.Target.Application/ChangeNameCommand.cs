using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;
using FluentResults;
using Mediator;

namespace EventSourcing.SourceGenerators.Target.Application;

public record ChangeNameCommand(Guid AggregateId, string Name) : IRequest<Result>;

public class ChangeNameCommandHandler : IRequestHandler<ChangeNameCommand, Result>
{
    private readonly IRepository<MyTestAggregate> _repository;

    public ChangeNameCommandHandler(IRepository<MyTestAggregate> repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result> Handle(ChangeNameCommand request, CancellationToken cancellationToken)
    {
        // ... do validation in the pipeline or here ...
        // ... check if the name is not already taken by using a read-projections-repository or a database context ...

        var updateResult = await _repository.UpdateAsync(request.AggregateId, (aggregate) => [new ChangedNameEvent(aggregate.Id, request.Name, DateTimeOffset.UtcNow)], cancellationToken);
        if (updateResult.IsFailed)
            return new Error("Failed to update aggregate").CausedBy(updateResult.Errors);
        
        // var updatedAggregate = updateResult.Value;
        
        return Result.Ok();
    }
}

