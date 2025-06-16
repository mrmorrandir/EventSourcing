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
        var repositories = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsRepositoryCandidate(s),
                transform: (ctx, _) => GetRepositoryInfo(ctx))
            .Where(info => info is not null);

        context.RegisterSourceOutput(repositories.Collect(), (spc, infos) =>
        {
            if (infos.IsDefaultOrEmpty)
                return;
            foreach (var info in infos)
            {
                var repositorySource = CreateRepositorySource(info!);
                spc.AddSource($"{info!.RepositoryNamespace}.{info!.AggregateName}Repository.g.cs", SourceText.From(repositorySource, Encoding.UTF8));
            }
            
            var repositoryDependencyInjectionSource = CreateRepositoryDependencyInjectionSource(infos!);
            spc.AddSource($"RepositoryDependencyInjection.g.cs", SourceText.From(repositoryDependencyInjectionSource, Encoding.UTF8));
        });
        
        // Create a source file for all the repositories together - a Dependency Injection class that registers them
        context.RegisterSourceOutput(repositories.Collect(), (spc, infos) =>
        {
            if (infos.IsDefaultOrEmpty)
                return;
            
        });
    }
    private static bool IsRepositoryCandidate(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Must be partial
        if (!classDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
            return false;

        // Must have a base list (implements or inherits)
        if (classDecl.BaseList == null)
            return false;

        // Must implement IAggregateRepository<T>
        return classDecl.BaseList.Types
            .Any(t => t.Type.ToString().StartsWith("IRepository<"));
    }
    
    private static RepositoryInfo? GetRepositoryInfo(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = model.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
        if (classSymbol == null)
            return null;

        // Find IAggregateRepository<T>
        var repoInterface = classSymbol.AllInterfaces
            .FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString() == "EventSourcing.Repositories.IRepository<TAggregate>");

        if (repoInterface == null)
            return null;

        var aggregateType = repoInterface.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        if (aggregateType == null)
            return null;

        return new RepositoryInfo
        {
            AggregateNamespace = aggregateType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
            AggregateName = aggregateType.Name,
            AggregateFullName = aggregateType.ToDisplayString(),
            RepositoryNamespace = classSymbol.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
            RepositoryName = classSymbol.Name,
            RepositoryFullName = classSymbol.ToDisplayString(),
        };
    }

    private static string CreateRepositorySource(RepositoryInfo info)
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
        if (!string.IsNullOrEmpty(info.AggregateNamespace))
            sb.AppendLine($"using {info.AggregateNamespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {info.RepositoryName} : Repository<{info.AggregateName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {info.RepositoryName}(IEventStore eventStore, ISerializationRegistry<{info.AggregateName}> serializationRegistry, IAggregator<{info.AggregateName}> aggregator, IEnumerable<IProjector<{info.AggregateName}>> projectors) : base(eventStore, serializationRegistry, aggregator, projectors) {{ }}");
        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private static string CreateRepositoryDependencyInjectionSource(ImmutableArray<RepositoryInfo> aggregateInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using EventSourcing.Repositories;");
        
        var namespaces = new List<string>();
        namespaces.AddRange(aggregateInfos.Where(x => !string.IsNullOrWhiteSpace(x.AggregateNamespace)).Select(x => x.AggregateNamespace));
        namespaces.AddRange(aggregateInfos.Where(x => !string.IsNullOrWhiteSpace(x.RepositoryNamespace)).Select(x => x.RepositoryNamespace));
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
        foreach (var info in aggregateInfos)
        {
            sb.AppendLine($"    /// <item>IRepository&lt;{info.AggregateName}&gt; (Implementation: <see cref=\"{info.RepositoryName}\"/>) and</item>");
            sb.AppendLine($"    /// <item>Repository&lt;{info.AggregateName}&gt; (Implementation: <see cref=\"{info.RepositoryName}\"/>) and</item>");
            sb.AppendLine($"    /// <item><see cref=\"{info.RepositoryName}\"/></item>");
        }

        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// It is recommended to use the interfaces that were registered in your application.");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void AddRepositories(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var info in aggregateInfos)
            sb.AppendLine($"        services.Add{info.RepositoryName}();");

        sb.AppendLine("    }");
        
        foreach (var info in aggregateInfos)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the <see cref=\"{info.RepositoryName}\"/> in the service collection.</para>");
            sb.AppendLine("    /// <para>In order to register all repositories use the <see cref=\"AddRepositories\"/> method.</para>");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static void Add{info.RepositoryName}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine($"        services.AddScoped<{info.RepositoryName}>();");
            sb.AppendLine($"        services.AddScoped<Repository<{info.AggregateName}>, {info.RepositoryName}>(sp => sp.GetRequiredService<{info.RepositoryName}>());");
            sb.AppendLine($"        services.AddScoped<IRepository<{info.AggregateName}>, {info.RepositoryName}>(sp => sp.GetRequiredService<{info.RepositoryName}>());");
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private class RepositoryInfo 
    {
        public string AggregateNamespace = "";
        public string AggregateName = "";
        public string AggregateFullName = "";
        public string RepositoryNamespace = "";
        public string RepositoryName = "";
        public string RepositoryFullName = "";
    }
}