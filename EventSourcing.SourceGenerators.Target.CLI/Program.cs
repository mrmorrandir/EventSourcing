using EventSourcing.Contexts;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;
using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(options => options
    .UseInMemoryDatabase("Target-CLI-Database")
    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))); // Ignore transaction warnings for in-memory database
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddEventSourcing();

var app = builder.Build();

var repo = app.Services.GetRequiredService<MyTestAggregateRepository>();

var createResult = await repo.CreateAsync(() => new CreatedEvent(Guid.NewGuid(), "Test Name", "Test Description", DateTimeOffset.UtcNow), CancellationToken.None);
if (createResult.IsFailed)
    Console.WriteLine("Failed to create aggregate: " + createResult.Errors.First().Message);
    
var aggregate = createResult.Value;
Console.WriteLine("Created aggregate: " + aggregate);

var changeResult = await repo.UpdateAsync(aggregate.Id, agg => [new ChangedNameEvent(agg.Id, "Other Name", DateTimeOffset.UtcNow)], CancellationToken.None);
if (changeResult.IsFailed)
    Console.WriteLine("Failed to change aggregate: " + changeResult.Errors.First().Message);
else
    Console.WriteLine("Changed aggregate name to: " + changeResult.Value.Name);
    
aggregate = changeResult.Value;
Console.WriteLine("Current aggregate: " + aggregate);

var context = app.Services.GetRequiredService<IEventStoreDbContext>();
var events = context.Events.OrderBy(x => x.Created).ToList();
Console.WriteLine("Events in the store:");
foreach (var ev in events)
    Console.WriteLine($"- {ev.Schema} (StreamId: {ev.StreamId}, Version: {ev.Version}, Timestamp: {ev.Created}, Data: {ev.Data.Replace("\n", " ")})");