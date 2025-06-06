using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddEventSourcing();
builder.Services.AddAggregators();
builder.Services.AddRepositories();
builder.Services.AddSerialization();

var app = builder.Build();

var repo = app.Services.GetRequiredService<MyTestAggregateRepository>();

var aggregateId = Guid.NewGuid();

var createResult = await repo.CreateAsync(() => new CreatedEvent(aggregateId, "Test Name", "Test Description", DateTimeOffset.UtcNow), CancellationToken.None);
if (createResult.IsFailed)
    Console.WriteLine("Failed to create aggregate: " + createResult.Errors.First().Message);
    
var aggregate = createResult.Value;

var changeResult = await repo.UpdateAsync(aggregateId, aggregate => [new ChangedNameEvent(aggregate.Id, "Other Name", DateTimeOffset.UtcNow)], CancellationToken.None);
if (changeResult.IsFailed)
    Console.WriteLine("Failed to change aggregate: " + changeResult.Errors.First().Message);
else
    Console.WriteLine("Changed aggregate name to: " + changeResult.Value.Name);