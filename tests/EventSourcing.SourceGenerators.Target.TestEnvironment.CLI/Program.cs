using EventSourcing.Contexts;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyTestAggregateRepository = EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2.MyTestAggregateRepository;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(options => options
    .UseInMemoryDatabase("TestEnvironment")
    // Configure the context to ignore transaction warnings
    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))); 
builder.Services.AddScoped<IEventStoreX, EventStoreX>();
builder.Services.AddScoped<MyTestAggregateRepository>();
builder.Services.AddScoped<IRepository<MyTestAggregate>>(sp => sp.GetRequiredService<MyTestAggregateRepository>());
builder.Services.AddScoped<IAggregator<MyTestAggregate>, MyTestAggregateAggregator>();
builder.Services.AddScoped<ISerializationRegistry<MyTestAggregate>, MyTestAggregateSerializationRegistry>();
builder.Services.AddProjections();

var app = builder.Build();

using var scope = app.Services.CreateScope(); 

var repositoryX = scope.ServiceProvider.GetRequiredService<MyTestAggregateRepository>();

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

// var updateResult2 = await repositoryX.UpdateAsync(aggregate.Id, a => 
// [
//     new DeletedEvent(aggregate.Id, DateTimeOffset.UtcNow)
// ], CancellationToken.None);
//
// if (updateResult2.IsFailed)
// {
//     Console.WriteLine(updateResult2);
//     return;
// }
//
// aggregate = updateResult2.Value;
// Console.WriteLine($"Deleted aggregate: {aggregate}");

var updateResult3 = await repositoryX.UpdateAsync(aggregate.Id, a =>
{
    //throw new Exception("Something went terribly wrong!");

    return [];
}, CancellationToken.None);

if (updateResult3.IsFailed)
{
    Console.WriteLine($"Update failed: {updateResult3}");
    return;
}

aggregate = updateResult3.Value;


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
