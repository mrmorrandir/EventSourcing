using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

[UseStateRepository(true)]
// TODO: Implement an Attribute to let the generator create more Projector classes with specific names for specific purposes (on the same aggregate - e.g., one for Name-Lookup, one for Summary etc.).
// TODO: Implement an Attribute to let the generator create specific Projection classes for specific events and purposes (e.g., only the CreatedEvent and the ChangedEvent in order to keep a list of "Name"s).
// For both todos the DependencyInjection is created automatically so that everything is ready to use, and the user only has to implement the logic in the Projector/Projection (partial) classes.
public partial class MyTestAggregateRepository: IRepository<MyTestAggregate>
{
    
}