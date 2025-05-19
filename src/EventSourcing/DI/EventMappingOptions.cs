using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.DI;

public record EventMappingOptions(IServiceCollection Services, IEnumerable<Type> CoveredEvents, IEnumerable<Type>? UncoveredEvents = null);