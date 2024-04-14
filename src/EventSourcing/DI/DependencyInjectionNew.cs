namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionNew
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, Action<EventSourcingOptionsBuilder>? options)
    {
        var builder = new EventSourcingOptionsBuilder(services);
        options?.Invoke(builder);
        builder.Build();
        return services;
    }
}