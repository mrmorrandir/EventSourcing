using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators.Repositories;

[Generator]
public class RepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var eventSourcingInfosProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => InfoProvider.IsRepositoryCandidate(s),
                transform: (ctx, _) => InfoProvider.GetEventSourcingInfo(ctx))
            .Where(info => info is not null);
        
        context.RegisterSourceOutput(eventSourcingInfosProvider.Collect(), (spc, eventSourcingInfos) =>
        {
            if (eventSourcingInfos.IsDefaultOrEmpty)
                return;
        
            foreach (var eventSourcingInfo in eventSourcingInfos)
            {
                var repositorySource = CreateRepositorySource(eventSourcingInfo!);
                spc.AddSource($"{eventSourcingInfo!.Repository.SaveFullNameForFiles}.g.cs", SourceText.From(repositorySource, Encoding.UTF8));
            }
            foreach (var eventSourcingInfo in eventSourcingInfos.Where(esi => esi!.StateRepository.Create))
            {
                var stateRepositorySource = CreateStateRepositorySource(eventSourcingInfo!);
                spc.AddSource($"{eventSourcingInfo!.StateRepository.SaveFullNameForFiles}.g.cs", SourceText.From(stateRepositorySource, Encoding.UTF8));
            }
            
            var repositoryDependencyInjectionSource = CreateRepositoryDependencyInjectionSource(eventSourcingInfos!);
            spc.AddSource($"RepositoryDependencyInjection.g.cs", SourceText.From(repositoryDependencyInjectionSource, Encoding.UTF8));
        });
        
        // Create a source file for all the repositories together - a Dependency Injection class that registers them
        context.RegisterSourceOutput(eventSourcingInfosProvider.Collect(), (spc, infos) =>
        {
            if (infos.IsDefaultOrEmpty)
                return;
            
        });
    }
    
    private static string CreateRepositorySource(InfoProvider.EventSourcingInfo eventSourcingInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Repositories;");
        sb.AppendLine("using EventSourcing.Stores;");
        sb.AppendLine("using EventSourcing.Mappers;");
        sb.AppendLine("using EventSourcing.Projections;");
        if (!string.IsNullOrEmpty(eventSourcingInfo.Aggregate.Namespace))
            sb.AppendLine($"using {eventSourcingInfo.Aggregate.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {eventSourcingInfo.Repository.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {eventSourcingInfo.Repository.SaveNameForCode} : Repository<{eventSourcingInfo.Aggregate.Name}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {eventSourcingInfo.Repository.SaveNameForCode}(IEventStore eventStore, ISerializationRegistry<{eventSourcingInfo.Aggregate.Name}> serializationRegistry, IAggregator<{eventSourcingInfo.Aggregate.Name}> aggregator, IEnumerable<IProjector<{eventSourcingInfo.Aggregate.Name}>> projectors) : base(eventStore, serializationRegistry, aggregator, projectors) {{ }}");
        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private static string CreateStateRepositorySource(InfoProvider.EventSourcingInfo eventSourcingInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Repositories;");
        sb.AppendLine("using EventSourcing.Stores;");
        sb.AppendLine("using EventSourcing.Mappers;");
        sb.AppendLine("using EventSourcing.Projections;");
        if (!string.IsNullOrEmpty(eventSourcingInfo.Aggregate.Namespace))
            sb.AppendLine($"using {eventSourcingInfo.Aggregate.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {eventSourcingInfo.StateRepository.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {eventSourcingInfo.StateRepository.SaveNameForCode} : StateRepository<{eventSourcingInfo.Aggregate.Name}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {eventSourcingInfo.StateRepository.SaveNameForCode}(IStateStore stateStore) : base(stateStore) {{ }}");
        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private static string CreateRepositoryDependencyInjectionSource(ImmutableArray<InfoProvider.EventSourcingInfo> eventSourcingInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using EventSourcing.Repositories;");
        
        var namespaces = new List<string>();
        namespaces.AddRange(eventSourcingInfos.Where(x => !string.IsNullOrWhiteSpace(x.Aggregate.Namespace)).Select(x => x.Aggregate.Namespace));
        namespaces.AddRange(eventSourcingInfos.Where(x => !string.IsNullOrWhiteSpace(x.Repository.Namespace)).Select(x => x.Repository.Namespace));
        foreach (var ns in namespaces.Distinct())
            sb.AppendLine($"using {ns};");
        
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("public static partial class RepositoryDependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// <para>Registers all repositories in the service collection.</para>");
        sb.AppendLine("    /// <para>Use this method in your infrastructure dependency injection to register all repositories.</para>");
        sb.AppendLine("    /// <para>");
        sb.AppendLine("    /// Repositories that will be registered:");
        
        sb.AppendLine("    /// <list type=\"bullet\">");
        
        foreach (var info in eventSourcingInfos)
        {
            sb.AppendLine($"    /// <item>IRepository&lt;{info.Aggregate.Name}&gt; (Implementation: <see cref=\"{info.Repository.Name}\"/>) and</item>");
            sb.AppendLine($"    /// <item>Repository&lt;{info.Aggregate.Name}&gt; (Implementation: <see cref=\"{info.Repository.Name}\"/>) and</item>");
            sb.AppendLine($"    /// <item><see cref=\"{info.Repository.Name}\"/></item>");
        }
        foreach (var info in eventSourcingInfos.Where(esi => esi.StateRepository.Create))
        {
            sb.AppendLine($"    /// <item>IStateRepository&lt;{info.Aggregate.Name}&gt; (Implementation: <see cref=\"{info.StateRepository.Name}\"/>) and</item>");
            sb.AppendLine($"    /// <item>StateRepository&lt;{info.Aggregate.Name}&gt; (Implementation: <see cref=\"{info.StateRepository.Name}\"/>) and</item>");
            sb.AppendLine($"    /// <item><see cref=\"{info.StateRepository.Name}\"/></item>");
        }

        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// It is recommended to use the interfaces that were registered in your application.");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void AddRepositories(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var info in eventSourcingInfos)
            sb.AppendLine($"        services.Add{info.Repository.Name}();");
        foreach (var info in eventSourcingInfos.Where(esi => esi.StateRepository.Create))
            sb.AppendLine($"        services.Add{info.StateRepository.Name}();");

        sb.AppendLine("    }");

        foreach (var info in eventSourcingInfos)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the <see cref=\"{info.Repository.Name}\"/> in the service collection.</para>");
            sb.AppendLine("    /// <para>To register all repositories use the <see cref=\"AddRepositories\"/> method.</para>");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static void Add{info.Repository.Name}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine($"        services.AddScoped<{info.Repository.Name}>();");
            sb.AppendLine($"        services.AddScoped<Repository<{info.Aggregate.Name}>, {info.Repository.Name}>(sp => sp.GetRequiredService<{info.Repository.Name}>());");
            sb.AppendLine($"        services.AddScoped<IRepository<{info.Aggregate.Name}>, {info.Repository.Name}>(sp => sp.GetRequiredService<{info.Repository.Name}>());");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        foreach (var info in eventSourcingInfos.Where(esi => esi.StateRepository.Create))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the <see cref=\"{info.StateRepository.Name}\"/> in the service collection.</para>");
            sb.AppendLine("    /// <para>To register all repositories use the <see cref=\"AddRepositories\"/> method.</para>");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static void Add{info.StateRepository.Name}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine($"        services.AddScoped<{info.StateRepository.Name}>();");
            sb.AppendLine($"        services.AddScoped<StateRepository<{info.Aggregate.Name}>, {info.StateRepository.Name}>(sp => sp.GetRequiredService<{info.StateRepository.Name}>());");
            sb.AppendLine($"        services.AddScoped<IStateRepository<{info.Aggregate.Name}>, {info.StateRepository.Name}>(sp => sp.GetRequiredService<{info.StateRepository.Name}>());");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        sb.AppendLine("}");
        
        return sb.ToString();
    }
}