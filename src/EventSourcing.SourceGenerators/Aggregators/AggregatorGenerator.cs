using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators.Aggregators;

[Generator]
public class AggregatorGenerator : IIncrementalGenerator
{
    private static readonly Regex _versionSuffixRegex = new Regex(@"-v[0-9]+$");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var aggregates = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => InfoProvider.IsRepositoryCandidate(s),
                (ctx, _) => InfoProvider.GetEventSourcingInfo(ctx))
            .Where(info => info is not null);
        
        context.RegisterSourceOutput(aggregates.Collect(), (spc, eventSourcingInfos) =>
        {
            if (eventSourcingInfos.IsDefaultOrEmpty)
                return;
            
            foreach (var eventSourcingInfo in eventSourcingInfos)
            {
                var aggregatorSource = CreateAggregatorSource(eventSourcingInfo!);
                spc.AddSource($"{eventSourcingInfo!.Repository.Namespace}.{eventSourcingInfo!.Aggregate.SaveNameForFiles}Aggregator.g.cs", SourceText.From(aggregatorSource, Encoding.UTF8));
            }
            
            var dependencyInjectionSource = CreateAggregatorsDependencyInjectionSource(eventSourcingInfos!);
            spc.AddSource("AggregatorsDependencyInjection.g.cs", SourceText.From(dependencyInjectionSource, Encoding.UTF8));
        });
    }

    private static string CreateAggregatorSource(InfoProvider.EventSourcingInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using FluentResults;");

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.Aggregate.Namespace))
            namespaces.Add(info.Aggregate.Namespace);
        namespaces.AddRange(info.Aggregate.CreateEvents.Where(em => !string.IsNullOrWhiteSpace(em.Namespace)).Select(x => x.Namespace));
        namespaces.AddRange(info.Aggregate.ApplyEvents.Where(em => !string.IsNullOrWhiteSpace(em.Namespace)).Select(x => x.Namespace));
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Repository.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {info.Aggregate.SaveNameForCode}Aggregator : IAggregator<{info.Aggregate.Name}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public Result<{info.Aggregate.Name}> CreateFromEvent(IEvent @event)");
        sb.AppendLine("    {");
        sb.AppendLine("        return @event switch");
        sb.AppendLine("        {");
        
        foreach (var eventInfo in info.Aggregate.CreateEvents)
            sb.AppendLine($"            {eventInfo.Name} {eventInfo.VariableName} when @event.GetType() == typeof({eventInfo.Name}) => CreateFromEvent({eventInfo.VariableName}),");
        
        sb.AppendLine("            _ => Result.Fail($\"Unknown event type: {@event.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        foreach (var eventInfo in info.Aggregate.CreateEvents)
        {
            sb.AppendLine($"    public Result<{info.Aggregate.Name}> CreateFromEvent({eventInfo.Name} @event)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return Result.Try(() => {info.Aggregate.Name}.Create(@event));");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        sb.AppendLine($"    public Result<{info.Aggregate.Name}> ApplyEvent({info.Aggregate.Name} aggregate, IEvent @event)");
        sb.AppendLine("    {");
        sb.AppendLine("        return @event switch");
        sb.AppendLine("        {");
        
        foreach (var eventInfo in info.Aggregate.ApplyEvents)
            sb.AppendLine($"            {eventInfo.Name} {eventInfo.VariableName} when @event.GetType() == typeof({eventInfo.Name}) => ApplyEvent(aggregate, {eventInfo.VariableName}),");
        
        sb.AppendLine("            _ => Result.Fail($\"Unknown event type: {@event.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        
        foreach (var eventInfo in info.Aggregate.ApplyEvents)
        {
            sb.AppendLine($"    public Result<{info.Aggregate.Name}> ApplyEvent({info.Aggregate.Name} aggregate, {eventInfo.Name} @event)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return Result.Try(() => aggregate.Apply(@event));");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        sb.AppendLine("}");

        return sb.ToString();
    }
    
    private static string CreateAggregatorsDependencyInjectionSource(ImmutableArray<InfoProvider.EventSourcingInfo> eventSourcingInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using EventSourcing;");
        
        var namespaces = new List<string>();
        namespaces.AddRange(eventSourcingInfos.Where(x => !string.IsNullOrWhiteSpace(x.Aggregate.Namespace)).Select(x => x.Aggregate.Namespace));
        namespaces.AddRange(eventSourcingInfos.Where(x => !string.IsNullOrWhiteSpace(x.Repository.Namespace)).Select(x => x.Repository.Namespace));
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");

        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("public static partial class AggregatorsDependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// <para>Registers all aggregators in the service collection.</para>");
        sb.AppendLine("    /// <para>Use this method in your infrastructure dependency injection to register all aggregators.</para>");
        sb.AppendLine("    /// <para>");
        sb.AppendLine("    /// Aggregators that will be registered:");
        
        sb.AppendLine("    /// <list type=\"bullet\">");
        foreach (var info in eventSourcingInfos)
            sb.AppendLine($"    /// <item>IAggregator&lt;{info.Aggregate.Name}&gt; (Implementation: <see cref=\"{info.Aggregate.Name}Aggregator\"/>)</item>");
        
        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void AddAggregators(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var info in eventSourcingInfos)
            sb.AppendLine($"        services.Add{info.Aggregate.SaveNameForCode}Aggregator();");

        sb.AppendLine("    }");
        
        foreach (var info in eventSourcingInfos) 
        {
            sb.AppendLine();    
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the {info.Aggregate.Name} aggregator in the service collection.</para>");
            sb.AppendLine($"    /// <para>In order to register all aggregators use the <see cref=\"AddAggregators\"/> method.</para>");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static IServiceCollection Add{info.Aggregate.SaveNameForCode}Aggregator(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine($"        services.AddSingleton<IAggregator<{info.Aggregate.Name}>, {info.Aggregate.SaveNameForCode}Aggregator>();");
            sb.AppendLine($"        return services;");
            sb.AppendLine("    }");
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        
        return sb.ToString();
    }
}