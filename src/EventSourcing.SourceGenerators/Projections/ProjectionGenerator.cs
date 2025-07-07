using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators.Projections;

[Generator]
public partial class ProjectionGenerator : IIncrementalGenerator
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
                foreach (var projection in eventSourcingInfo!.Projections)
                {
                    var projectionSource = CreateProjectionSource(eventSourcingInfo!, projection);
                    spc.AddSource($"{projection.SaveFullNameForFiles}.g.cs", SourceText.From(projectionSource, Encoding.UTF8));
                }
            }
        
            foreach (var eventSourcingInfo in eventSourcingInfos)
            {
                var projectorSource = CreateProjectorSource(eventSourcingInfo!);
                spc.AddSource($"{eventSourcingInfo!.Projector.SaveFullNameForFiles}.g.cs", SourceText.From(projectorSource, Encoding.UTF8));

                if (eventSourcingInfo.StateProjector.Create)
                {
                    var stateProjectorSource = CreateStateProjectorSource(eventSourcingInfo!);
                    spc.AddSource($"{eventSourcingInfo!.StateProjector.SaveFullNameForFiles}.g.cs", SourceText.From(stateProjectorSource, Encoding.UTF8));
                }
            }
            
            var dependencyInjectionSource = CreateProjectorsDependencyInjectionSource(eventSourcingInfos!);
            spc.AddSource("ProjectorsDependencyInjection.g.cs", SourceText.From(dependencyInjectionSource, Encoding.UTF8));
        });
    }

    private static string CreateProjectionSource(InfoProvider.EventSourcingInfo eventSourcingInfo, InfoProvider.Projection projection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventSourcingInfo.Aggregate.Namespace))
            namespaces.Add(eventSourcingInfo.Aggregate.Namespace);
        if (!string.IsNullOrWhiteSpace(projection.Namespace))
            namespaces.Add(projection.Namespace);
        if (!string.IsNullOrWhiteSpace(projection.Event.Namespace))
            namespaces.Add(projection.Event.Namespace);
        foreach (var ns in namespaces.Distinct())
            sb.AppendLine($"using {ns};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {projection.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {projection.SaveNameForCode} : AbstractProjection<{eventSourcingInfo.Aggregate.Name}, {projection.Event.Name}>");
        sb.AppendLine("{");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string CreateProjectorSource(InfoProvider.EventSourcingInfo eventSourcingInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");
        sb.AppendLine("using FluentResults;");
        

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventSourcingInfo.Aggregate.Namespace))
            namespaces.Add(eventSourcingInfo.Aggregate.Namespace);
        namespaces.AddRange(eventSourcingInfo.Aggregate.Events.Where(x => !string.IsNullOrWhiteSpace(x.Namespace)).Select(x => x.Namespace));
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {eventSourcingInfo.Repository.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {eventSourcingInfo.Projector.SaveNameForCode} : IProjector<{eventSourcingInfo.Aggregate.Name}>");
        sb.AppendLine("{");
        
        foreach (var projection in eventSourcingInfo.Projections)
            sb.AppendLine($"    private readonly {projection.SaveNameForCode} {projection.FieldName};");
        
        sb.AppendLine();
        sb.AppendLine($"    public {eventSourcingInfo.Projector.SaveNameForCode}(");
        foreach (var projection in eventSourcingInfo.Projections)
        {
            sb.Append($"        {projection.SaveNameForCode} {projection.VariableName}");
            sb.AppendLine(projection != eventSourcingInfo.Projections.Last() ? "," : ")");
        }
        sb.AppendLine("    {");
        
        foreach (var projection in eventSourcingInfo.Projections)
            sb.AppendLine($"        {projection.FieldName} = {projection.VariableName};");
        
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<Result> ProjectAsync({eventSourcingInfo.Aggregate.Name} state, IEvent @event, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return @event.GetType() switch");
        sb.AppendLine("        {");
        
        foreach (var projection in eventSourcingInfo.Projections)
            sb.AppendLine($"            {{ }} type when type == typeof({projection.Event.Name}) => await {projection.FieldName}.ProjectAsync(state, ({projection.Event.Name})@event, cancellationToken),");
        
        sb.AppendLine("            _ => Result.Fail(\"No projection found for event type \" + @event.GetType().Name)");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    
    private static string CreateStateProjectorSource(InfoProvider.EventSourcingInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");
        sb.AppendLine("using EventSourcing.Stores;");
        sb.AppendLine("using EventSourcing.Mappers;");
        sb.AppendLine("using FluentResults;");
        

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.Aggregate.Namespace))
            namespaces.Add(info.Aggregate.Namespace);
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Repository.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {info.StateProjector.SaveNameForCode} : IProjector<{info.Aggregate.Name}>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IStateStore _stateStore;");
        sb.AppendLine("    private readonly ISerializationRegistry<" + info.Aggregate.Name + "> _serializationRegistry;");
        sb.AppendLine();
        sb.AppendLine($"    public {info.StateProjector.SaveNameForCode}(IStateStore stateStore, ISerializationRegistry<{info.Aggregate.Name}> serializationRegistry)");
        sb.AppendLine("    {");
        sb.AppendLine("        _stateStore = stateStore;");
        sb.AppendLine("        _serializationRegistry = serializationRegistry;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<Result> ProjectAsync({info.Aggregate.Name} state, IEvent @event, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var serializeResult = _serializationRegistry.Serialize(state);");
        sb.AppendLine("        if (serializeResult.IsFailed)");
        sb.AppendLine($"            return new Error(\"Failed to serialize aggregate of type {info.Aggregate.Name}\").CausedBy(serializeResult.Errors);");
        sb.AppendLine();
        sb.AppendLine("        var serializedState = serializeResult.Value;");
        sb.AppendLine("        var stateEntity = new StateEntity(state.Id, serializedState.Schema, serializedState.Data);");
        sb.AppendLine("        var saveResult = await _stateStore.SaveStateAsync(stateEntity, cancellationToken);");
        sb.AppendLine("        if (saveResult.IsFailed)");
        sb.AppendLine("            return new Error(\"Failed to save state\").CausedBy(saveResult.Errors);");
        sb.AppendLine();
        sb.AppendLine("        return Result.Ok();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    
    private static string CreateProjectorsDependencyInjectionSource(ImmutableArray<InfoProvider.EventSourcingInfo> eventSourcingInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        
        var namespaces = new List<string>();
        namespaces.AddRange(eventSourcingInfos.Where(x => !string.IsNullOrWhiteSpace(x.Aggregate.Namespace)).Select(x => x.Aggregate.Namespace));
        namespaces.AddRange(eventSourcingInfos.Where(x => !string.IsNullOrWhiteSpace(x.Repository.Namespace)).Select(x => x.Repository.Namespace));
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");

        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("public static partial class ProjectorsDependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// <para>Registers all projectors in the service collection.</para>");
        sb.AppendLine("    /// <para>Use this method in your infrastructure dependency injection to register all projectors.</para>");
        sb.AppendLine("    /// <para>");
        sb.AppendLine("    /// Projectors that will be registered:");
        
        sb.AppendLine("    /// <list type=\"bullet\">");

        foreach (var eventSourcingInfo in eventSourcingInfos)
        {
            sb.AppendLine($"    /// <item>IProjector&lt;{eventSourcingInfo.Aggregate.Name.Replace("<", "&lt;").Replace(">", "&gt;")}&gt; (Implementation: <see cref=\"{eventSourcingInfo.Projector.SaveNameForCode}\"/>)</item>");
            if (eventSourcingInfo.StateRepository.Create)
                sb.AppendLine($"    /// <item>IProjector&lt;{eventSourcingInfo.Aggregate.Name.Replace("<", "&lt;").Replace(">", "&gt;")}&gt; (Implementation: <see cref=\"{eventSourcingInfo.StateProjector.SaveNameForCode}\"/>)</item>");
        }

        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void AddProjectors(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var eventSourcingInfo in eventSourcingInfos)
        {
            sb.AppendLine($"    services.Add{eventSourcingInfo.Projector.SaveNameForCode}();");
            if (eventSourcingInfo.StateRepository.Create)
                sb.AppendLine($"    services.Add{eventSourcingInfo.StateProjector.SaveNameForCode}();");
        }

        sb.AppendLine("    }");
        
        foreach (var eventSourcingInfo in eventSourcingInfos)
        {
            sb.AppendLine();    
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the {eventSourcingInfo.Aggregate.Name.Replace("<", "&lt;").Replace(">", "&gt;")} projector in the service collection.</para>");
            sb.AppendLine($"    /// <para>To register all projectors use the <see cref=\"AddProjectors\"/> method.</para>");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static IServiceCollection Add{eventSourcingInfo.Projector.SaveNameForCode}(this IServiceCollection services)");
            sb.AppendLine("    {");
            
            // Register all the projections for the projector
            foreach (var projection in eventSourcingInfo.Projections)
                sb.AppendLine($"        services.AddScoped<{projection.SaveNameForCode}>();");
            
            sb.AppendLine($"        services.AddScoped<IProjector<{eventSourcingInfo.Aggregate.Name}>, {eventSourcingInfo.Projector.SaveNameForCode}>();");
            sb.AppendLine($"        return services;");
            sb.AppendLine("    }");
            sb.AppendLine();
            if (eventSourcingInfo.StateRepository.Create)
            {
                sb.AppendLine();
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// <para>Registers the <see cref=\"{eventSourcingInfo.StateProjector.SaveNameForCode}\"/> in the service collection.</para>");
                sb.AppendLine($"    /// <para>To register all projectors use the <see cref=\"AddProjectors\"/> method.</para>");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public static IServiceCollection Add{eventSourcingInfo.StateProjector.SaveNameForCode}(this IServiceCollection services)");
                sb.AppendLine("    {");
                sb.AppendLine($"        services.AddScoped<IProjector<{eventSourcingInfo.Aggregate.Name}>, {eventSourcingInfo.StateProjector.SaveNameForCode}>();");
                sb.AppendLine("        return services;");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        
        return sb.ToString();
    }
}