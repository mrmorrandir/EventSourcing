# EventSourcing.Publishers.RabbitMQ

RabbitMQ event and state publishers for the EventSourcing framework.

Use this package when committed events or aggregate state should be published to
RabbitMQ as part of the repository projection pipeline.

## Installation

```xml
<PackageReference Include="EventSourcing" Version="2.0.0" />
<PackageReference Include="EventSourcing.SourceGenerators" Version="2.0.0" PrivateAssets="all" />
<PackageReference Include="EventSourcing.Publishers.RabbitMQ" Version="2.0.0" />
```

## Tutorial

Register the EventSourcing runtime and RabbitMQ publishing services.

```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.AddEventSourcing(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventStore"));
});

builder.Services.AddRabbitMqPublishing(options =>
{
    options.UseConnection("localhost", "guest", "guest");
    options.UseBaseExchangeName("myshop");
});
```

Register event or state publishing for an aggregate.

```csharp
using MyShop.Domain.Orders;

builder.Services.AddRabbitMqEventPublisher<Order>();
builder.Services.AddRabbitMqStatePublisher<Order>();
```

RabbitMQ publishers are registered as projectors. If your application also uses
generated read-model projections, you still need to implement the generated
partial projection classes and override `ProjectAsync`.

Initialize RabbitMQ exchanges after building the service provider.

```csharp
var app = builder.Build();

await app.Services.UseRabbitMqPublishing();
app.Services.UseEventSourcing();
```

When an aggregate repository saves events, the generated projector registration
allows RabbitMQ publishers to publish the event or current state. Event
publishing uses event schemas from the serialization registry, so the
source-generator package should be installed in the infrastructure project.
