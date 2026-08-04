using EventSourcing.Repositories;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.Processes;

[UseStateRepository(true)]
public partial class ProcessRepository : IRepository<Process<LubricantData>>
{
    
}