using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;
using FluentResults;
using Mediator;

namespace EventSourcing.SourceGenerators.Target.Application;

public record CreateCommand(string Name, string Description) : IRequest<Result<Guid>>;

public class CreateCommandHandler : IRequestHandler<CreateCommand, Result<Guid>>
{
    private readonly IRepository<MyTestAggregate> _repository;

    public CreateCommandHandler(IRepository<MyTestAggregate> repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<Guid>> Handle(CreateCommand request, CancellationToken cancellationToken)
    {
        // ... do validation in the pipeline or here ...
        // ... check if the name is not already taken by using a read-projections-repository or a database context ...
        
        var createResult = await _repository.CreateAsync(() => new CreatedEvent(Guid.NewGuid(), request.Name, request.Description, DateTimeOffset.UtcNow), cancellationToken);
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);
        
        var aggregate = createResult.Value;
        return Result.Ok(aggregate.Id);
    }
}