namespace Microsoft.Extensions.DependencyInjection;

public record EventMappingOptions(IServiceCollection Services, IEnumerable<Type> CoveredEvents, IEnumerable<Type>? UncoveredEvents = null);