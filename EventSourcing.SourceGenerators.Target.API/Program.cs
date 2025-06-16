using EventSourcing.Contexts;
using EventSourcing.SourceGenerators.Target.API.Common;
using EventSourcing.SourceGenerators.Target.Application;
using EventSourcing.Stores;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventSourcing(options => options
    .UseInMemoryDatabase("MyTestDatabase")
    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/my-test", async (IMediator mediator, CreateCommand createCommand) => await mediator.Send(createCommand).ToWebResult())
    .Produces<Guid>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .WithTags("MyTest")
    .WithName("MyTest_Create")
    .WithDescription("Creates a new MyTest object with the specified name and description.");

app.MapPatch("/my-test/name", async (IMediator mediator, ChangeNameCommand changeNameCommand) => await mediator.Send(changeNameCommand).ToWebResult())
    .Produces(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .WithTags("MyTest")
    .WithName("MyTest_ChangeName")
    .WithDescription("Changes the name of an existing MyTest object identified by its aggregate ID.");

app.Run();
