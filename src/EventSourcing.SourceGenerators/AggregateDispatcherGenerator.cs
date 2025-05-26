using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators;

[Generator]
public class AggregateDispatcherGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var aggregates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsAggregateCandidate(s),
                transform: (ctx, _) => GetAggregateInfo(ctx))
            .Where(info => info is not null);

        context.RegisterSourceOutput(aggregates, (spc, info) =>
        {
            var source = GenerateDispatcher(info!);
            spc.AddSource($"{info!.AggregateName}Dispatcher.g.cs", SourceText.From(source, Encoding.UTF8));
        });

        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("IAggregate.g.cs", SourceText.From(
                "namespace EventSourcing.Abstractions { public interface IAggregate { } }",
                Encoding.UTF8));
            ctx.AddSource("IAggregateEvent.g.cs", SourceText.From(
                "namespace EventSourcing.Abstractions { public interface IAggregateEvent { Guid Id { get; } } }",
                Encoding.UTF8));
        });
    }

    private static bool IsAggregateCandidate(SyntaxNode node)
    {
        return node is RecordDeclarationSyntax recordDeclarationSyntax &&
               recordDeclarationSyntax.BaseList?.Types.Any(baseType => baseType.ToString().Contains("IAggregate")) == true;
    }

    private static AggregateInfo? GetAggregateInfo(GeneratorSyntaxContext context)
    {
        var recordSyntax = (RecordDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var symbol = model.GetDeclaredSymbol(recordSyntax) as INamedTypeSymbol;
        if (symbol == null)
            return null;

        if (!symbol.AllInterfaces.Any(i => i.Name == "IAggregate"))
            return null;

        var applyMethods = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == "Apply" && !m.IsStatic && m.Parameters.Length == 1)
            .Select(m => new ApplyMethodInfo
            {
                ParameterType = m.Parameters[0].Type.ToDisplayString(),
                ReturnType = m.ReturnType.ToDisplayString()
            })
            .ToList();

        var createMethods = symbol.GetMembers()
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

        return new AggregateInfo
        {
            Namespace = symbol.ContainingNamespace.ToDisplayString(),
            AggregateName = symbol.Name,
            ApplyMethods = applyMethods,
            CreateMethods = createMethods
        };
    }

    private static string GenerateDispatcher(AggregateInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine($"namespace {info.Namespace}.Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {info.AggregateName}Dispatcher");
        sb.AppendLine("    {");

        if (info.ApplyMethods.Count > 0)
        {
            sb.AppendLine($"        public static {info.AggregateName} ApplyEvent({info.AggregateName} aggregate, object evt)");
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
            sb.AppendLine($"        public static {info.AggregateName} CreateFromEvent(object evt)");
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

    private static string GenerateRepository(AggregateInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine($"namespace {info.Namespace}.Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {info.AggregateName}Repository");
        sb.AppendLine("    {");

        if (info.CreateMethods.Count > 0)
        {
            sb.AppendLine($"        public static {info.AggregateName} CreateFromEvent(object evt)");
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