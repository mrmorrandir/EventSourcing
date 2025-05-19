using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.DI;

public record EventProjectionOptions(IServiceCollection Services, IEnumerable<Type> CoveredEvents, IEnumerable<Type>? UncoveredEvents);