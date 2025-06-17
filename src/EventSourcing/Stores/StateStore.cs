using EventSourcing.Contexts;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Stores;

public class StateStore : IStateStore
{
    private readonly IEventStoreDbContext _context;

    public StateStore(IEventStoreDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<StateEntity>>> GetStatesAsync(Guid? aggregateId = null, CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
            return new Error("The aggregate Id must not be empty. #MissingAggregateId");

        var query = _context.States.AsNoTracking();
        if (aggregateId.HasValue)
            query = query.Where(s => s.Id == aggregateId.Value);
        
        var statesResult = await Result.Try(() => query.ToListAsync(cancellationToken));
        if (statesResult.IsFailed)
        {
            return aggregateId.HasValue
                ? new Error($"Failed to retrieve state for aggregate with id '{aggregateId.Value}'. #FailedToRetrieveState")
                : new Error("Failed to retrieve states. #FailedToRetrieveAllStates");
        }
        
        if (aggregateId.HasValue && statesResult.Value.Count == 0)
            return new Error($"No state found for aggregate with id '{aggregateId.Value}'. #StateNotFound");
        
        return statesResult.Value;
    }

    public async Task<Result> SaveStateAsync(StateEntity state, CancellationToken cancellationToken = default)
    {
        if (state.Id == Guid.Empty)
            return new Error("The state Id must not be empty. #MissingStateId");

        // Check if the state already exists
        var existingStateResult = await Result.Try(() => _context.States.FirstOrDefaultAsync(x => x.Id == state.Id, cancellationToken));
        if (existingStateResult.IsFailed)
            return new Error($"Failed to check if state with id '{state.Id}' exists. #FailedToCheckStateExists");
        
        var existingState = existingStateResult.Value;
        if (existingState != null)
        {
            try
            {
                var transaction = await _context.BeginTransactionAsync(cancellationToken);
                _context.States.Remove(existingState);
                _context.States.Add(state);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Ok();
            } catch (Exception ex)
            {
                return new Error($"Failed to update state with id '{state.Id}'. #FailedToUpdateState").CausedBy(ex);
            }
        } 
        
        try
        {
            await _context.States.AddAsync(state, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return new Error($"Failed to save state with id '{state.Id}'. #FailedToSaveState").CausedBy(ex);
        }
    }
}