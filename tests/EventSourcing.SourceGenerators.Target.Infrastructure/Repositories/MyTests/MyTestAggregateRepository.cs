using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

[UseStateRepository(true)]
public partial class MyTestAggregateRepository: IRepository<MyTestAggregate>
{
    
}