using EventSourcing.Mappers;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.Repositories;

public class StateRepository<TAggregate> : IStateRepository<TAggregate> where TAggregate : IAggregate
{
    private static readonly string _schema = ToKebabCase(typeof(TAggregate).Name);
    private static readonly StateDeserializer<TAggregate> _deserializer = new();
    private readonly IStateStore _stateStore;
    
    public StateRepository(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }
    
    public async Task<Result<List<TAggregate>>> GetStatesAsync(Guid? aggregateId = null, long? offset = null, long? limit = null, CancellationToken cancellationToken = default)
    {
        var statesResult = await _stateStore.GetStatesAsync(aggregateId, _schema, offset, limit, cancellationToken);
        if (statesResult.IsFailed)
            return statesResult.ToResult();

        var states = statesResult.Value;
        var aggregates = new List<TAggregate>();
        foreach (var state in states)
        {
            var aggregateResult = Result.Try(() => _deserializer.Deserialize(state.Data));
            if (aggregateResult.IsFailed)
                return new Error($"Failed to deserialize state with id '{state.Id}' for aggregate '{typeof(TAggregate).Name}'. #FailedToDeserializeState");
            aggregates.Add(aggregateResult.Value);
        }
        return aggregates;
    }
    
    private static string ToKebabCase(string type) => string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
}