using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators;

//[Generator]
public class AggregateRepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var aggregates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsRepositoryCandidate(s),
                transform: (ctx, _) => GetAggregateInfo(ctx))
            .Where(info => info is not null);

        context.RegisterSourceOutput(aggregates, (spc, info) =>
        {
            var source = GenerateRepository(info!);
            spc.AddSource($"{info!.AggregateName}Repository.g.cs", SourceText.From(source, Encoding.UTF8));
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
            .Any(t => t.Type.ToString().StartsWith("IAggregateRepository<"));
    }
    
    private static AggregateInfo? GetAggregateInfo(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = model.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
        if (classSymbol == null)
            return null;

        // Find IAggregateRepository<T>
        var repoInterface = classSymbol.AllInterfaces
            .FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString() == "EventSourcing.Repositories.IAggregateRepository<TAggregate>");

        if (repoInterface == null)
            return null;

        var aggregateType = repoInterface.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        if (aggregateType == null)
            return null;

        // Analyze aggregateType for Apply and Create methods
        var applyMethods = aggregateType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == "Apply" && !m.IsStatic && m.Parameters.Length == 1)
            .Select(m => new ApplyMethodInfo
            {
                ParameterType = m.Parameters[0].Type.ToDisplayString(),
                ReturnType = m.ReturnType.ToDisplayString()
            })
            .ToList();

        var createMethods = aggregateType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == "Create" && m.IsStatic && m.Parameters.Length == 1)
            .Select(m => new CreateMethodInfo
            {
                ParameterType = m.Parameters[0].Type.ToDisplayString(),
                ReturnType = m.ReturnType.ToDisplayString()
            })
            .ToList();

        if (applyMethods.Count == 0 && createMethods.Count == 0)
            return null;
        
        
        // Use the repository class's namespace
        var repoNamespace = classSymbol.ContainingNamespace.ToDisplayString();

        return new AggregateInfo
        {
            Namespace = aggregateType.ContainingNamespace.ToDisplayString(),
            AggregateName = aggregateType.Name,
            ApplyMethods = applyMethods,
            CreateMethods = createMethods,
            RepositoryNamespace = repoNamespace,
        };
    }

    private static string GenerateRepository(AggregateInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Repositories;");
        sb.AppendLine();
        sb.AppendLine($"using {info.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {info.AggregateName}Repository");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly Dictionary<Guid, List<object>> _streams = new();");
        sb.AppendLine();
        sb.AppendLine($"        public {info.AggregateName}Repository() {{ }}");
        sb.AppendLine();
        sb.AppendLine($"        public {info.AggregateName} Get(Guid id)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!_streams.TryGetValue(id, out var events))");
        sb.AppendLine("                throw new InvalidOperationException($\"Aggregate with ID {id} not found.\");");
        sb.AppendLine();
        sb.AppendLine($"            {info.AggregateName}? aggregate = null;");
        sb.AppendLine("            foreach (var evt in events)");
        sb.AppendLine("                aggregate = aggregate == null ? CreateFromEvent(evt) : ApplyEvent(aggregate, evt);");
        sb.AppendLine();
        sb.AppendLine("            return aggregate ?? throw new InvalidOperationException($\"No events found for aggregate with ID {id}.\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public void Save(Guid id, IEnumerable<IEvent> events)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!_streams.ContainsKey(id))");
        sb.AppendLine("                _streams[id] = [];");
        sb.AppendLine("            foreach (var evt in events)");
        sb.AppendLine("                _streams[id].Add(evt);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public {info.AggregateName} SaveAndGet(Guid id, IEnumerable<IEvent> events)");
        sb.AppendLine("        {");
        sb.AppendLine("            Save(id, events);");
        sb.AppendLine("            return Get(id);");
        sb.AppendLine("        }");
        sb.AppendLine();

        if (info.ApplyMethods.Count > 0)
        {
            sb.AppendLine($"        private static {info.AggregateName} ApplyEvent({info.AggregateName} aggregate, object evt)");
            sb.AppendLine("        {");
            sb.AppendLine("            return evt switch");
            sb.AppendLine("            {");
            foreach (var method in info.ApplyMethods)
            {
                sb.AppendLine($"                {method.ParameterType} e => aggregate.Apply(e),");
            }
            sb.AppendLine("                _ => throw new InvalidOperationException($\"Unknown event type: {evt.GetType().Name}\")");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
        }

        if (info.CreateMethods.Count > 0)
        {
            sb.AppendLine($"        private static {info.AggregateName} CreateFromEvent(object evt)");
            sb.AppendLine("        {");
            sb.AppendLine("            return evt switch");
            sb.AppendLine("            {");
            foreach (var method in info.CreateMethods)
            {
                sb.AppendLine($"                {method.ParameterType} e => {info.AggregateName}.Create(e),");
            }
            sb.AppendLine("                _ => throw new InvalidOperationException($\"Unknown event type: {evt.GetType().Name}\")");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private class AggregateInfo 
    {
        public string Namespace = "";
        public string AggregateName = "";
        public List<ApplyMethodInfo> ApplyMethods = new();
        public List<CreateMethodInfo> CreateMethods = new();
        public string RepositoryNamespace = "";
    }

    private class ApplyMethodInfo
    {
        public string ParameterType = "";
        public string ReturnType = "";
    }

    private class CreateMethodInfo
    {
        public string ParameterType = "";
        public string ReturnType = "";
    }
}