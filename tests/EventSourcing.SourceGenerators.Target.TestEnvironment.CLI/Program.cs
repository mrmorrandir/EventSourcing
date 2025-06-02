using EventSourcing.Contexts;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(options => options.UseInMemoryDatabase("TestEnvironment"));
builder.Services.AddScoped<EventStoreX>();
builder.Services.AddScoped<MyTestAggregateRepositoryX>();

var app = builder.Build();

using var scope = app.Services.CreateScope(); 

var repositoryX = scope.ServiceProvider.GetRequiredService<MyTestAggregateRepositoryX>();

var createResult = await repositoryX.CreateAsync(() => new CreatedEvent(Guid.NewGuid(), "Test", "Test Description", DateTimeOffset.UtcNow), CancellationToken.None);
if (createResult.IsFailed)
{
    Console.WriteLine(createResult);
    return;
}
        
var aggregate = createResult.Value;
Console.WriteLine($"Created aggregate: {aggregate}");
        
var updateResult = await repositoryX.UpdateAsync(aggregate.Id, a => 
[
    new ChangedNameEvent(a.Id, "Neuer Name", DateTimeOffset.UtcNow), 
    new ChangedDescriptionEvent(a.Id, "Neue Beschreibung", DateTimeOffset.UtcNow)
], CancellationToken.None);

if (updateResult.IsFailed)
{
    Console.WriteLine(updateResult);
    return;
}
        
aggregate = updateResult.Value;
Console.WriteLine($"Updated aggregate: {aggregate}");

// List of events for the aggregate
var context = scope.ServiceProvider.GetRequiredService<IEventStoreDbContext>();
var events = await context.Events
    .Where(e => e.StreamId == aggregate.Id)
    .OrderBy(e => e.Version)
    .ToListAsync();
    
foreach (var evt in events)
{
    Console.WriteLine($"Event: {evt.Schema}, Version: {evt.Version}, Data: {evt.Data}");
}

public static class ResultExtensions
{
    // Return the full error hierarchy as a string
    public static string GetFullErrorMessage(this ResultBase result)
    {
        return string.Join(" -> ", result.Errors.Select(e => e.Message));
    }
}