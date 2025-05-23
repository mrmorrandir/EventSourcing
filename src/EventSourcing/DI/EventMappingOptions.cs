using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.DI;

public record EventMappingOptions(IServiceCollection Services, ImmutableArray<Type> CoveredEvents);