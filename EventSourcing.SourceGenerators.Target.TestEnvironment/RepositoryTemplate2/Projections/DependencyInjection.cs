using EventSourcing.SourceGenerators.Target.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2.Projections;

public static class DependencyInjection
{
    public static IServiceCollection AddProjections(this IServiceCollection services)
    {
        services.AddScoped<IProjector<MyTestAggregate>, MyTestAggregateProjector>();

        services.AddScoped<CreatedEventProjection>();
        services.AddScoped<ChangedNameEventProjection>();
        services.AddScoped<ChangedDescriptionEventProjection>();
        return services;
    }
}