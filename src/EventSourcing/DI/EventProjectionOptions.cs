namespace Microsoft.Extensions.DependencyInjection;

public record EventProjectionOptions(IServiceCollection Services, IEnumerable<Type> CoveredEvents, IEnumerable<Type>? UncoveredEvents);