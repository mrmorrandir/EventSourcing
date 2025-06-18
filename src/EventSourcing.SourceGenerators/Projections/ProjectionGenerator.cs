using System.Collections.Immutable;
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
                (s, _) => IsRepositoryCandidate(s),
                (ctx, _) => GetRepositoriesAggregateInfo(ctx))
            .Where(info => info is not null);

        context.RegisterSourceOutput(aggregates.Collect(), (spc, infos) =>
        {
            if (infos.IsDefaultOrEmpty)
                return;
            
            var allEventInfos = new List<EventInfo>();
            foreach (var aggregateInfo in infos)
            {
                var eventInfos = CreateEventInfos(aggregateInfo!);
                if (eventInfos.Length == 0)
                    return;

                foreach (var eventInfo in eventInfos)
                {
                    var projectionSource = CreateProjectionSource(aggregateInfo!, eventInfo);
                    spc.AddSource($"{aggregateInfo!.RepositoryNamespace}.{aggregateInfo!.AggregateName}{eventInfo.EventName}Projection.g.cs", SourceText.From(projectionSource, Encoding.UTF8));
                }
                
                var projectorSource = CreateProjectorSource(aggregateInfo!, [..eventInfos]);
                spc.AddSource($"{aggregateInfo!.RepositoryNamespace}.{aggregateInfo!.AggregateName}Projector.g.cs", SourceText.From(projectorSource, Encoding.UTF8));
                
                allEventInfos.AddRange(eventInfos);
            }

            foreach (var aggregateInfo in infos.Where(x => x!.CreateStateRepository))
            {
                var stateProjectorSource = CreateStateProjectorSource(aggregateInfo!);
                spc.AddSource($"{aggregateInfo!.RepositoryNamespace}.{aggregateInfo!.AggregateName}StateProjector.g.cs", SourceText.From(stateProjectorSource, Encoding.UTF8));
            }
            
            var dependencyInjectionSource = CreateProjectorsDependencyInjectionSource(infos!, [..allEventInfos]);
            spc.AddSource("ProjectorsDependencyInjection.g.cs", SourceText.From(dependencyInjectionSource, Encoding.UTF8));
        });
    }

    private static bool IsRepositoryCandidate(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Must be partial
        if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            return false;

        // Must have a base list (implements or inherits)
        if (classDecl.BaseList == null)
            return false;

        // Must implement IAggregateRepository<T>
        return classDecl.BaseList.Types
            .Any(t => t.Type.ToString().StartsWith("IRepository<"));
    }

    private static AggregateInfo? GetRepositoriesAggregateInfo(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var repositoryType = ModelExtensions.GetDeclaredSymbol(model, classSyntax) as INamedTypeSymbol;
        if (repositoryType is null)
            return null;

        // Find IRepository<T>
        var repoInterface = repositoryType.AllInterfaces
            .FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString() == "EventSourcing.Repositories.IRepository<TAggregate>");

        if (repoInterface is null)
            return null;

        var aggregateType = repoInterface.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        if (aggregateType is null)
            return null;

        // Analyze aggregateType for Apply and Create methods
       var applyMethods = aggregateType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m is { Name: "Apply", IsStatic: false, Parameters.Length: 1 })
            .Select(m => {
                var eventType = m.Parameters[0].Type;
                return new MutateMethodInfo
                {
                    MethodName = m.Name,
                    MethodFullName = m.ToDisplayString(),
                    AggregateFullName = aggregateType.ToDisplayString(),
                    EventNamespace = eventType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
                    EventName = eventType.Name,
                    EventFullName = eventType.ToDisplayString(),
                    ReturnType = m.ReturnType.ToDisplayString()
                };
            })
            .ToList();
        
        var createMethods = aggregateType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m is { Name: "Create", IsStatic: true, Parameters.Length: 1 })
            .Select(m => {
                var eventType = m.Parameters[0].Type;
                return new MutateMethodInfo
                {
                    MethodName = m.Name,
                    MethodFullName = m.ToDisplayString(),
                    AggregateFullName = aggregateType.ToDisplayString(),
                    EventNamespace = eventType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
                    EventName = eventType.Name,
                    EventFullName = eventType.ToDisplayString(),
                    ReturnType = m.ReturnType.ToDisplayString()
                };
            })
            .ToList();

        if (applyMethods.Count == 0 && createMethods.Count == 0)
            return null;

        // Check if the Repository has a [UseStateRepository] attribute
        bool createStateRepository = false;
        var useStateRepositoryAttribute = repositoryType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "EventSourcing.Repositories.UseStateRepositoryAttribute");
        if (useStateRepositoryAttribute != null)
            createStateRepository = useStateRepositoryAttribute.ConstructorArguments.Length > 0 && useStateRepositoryAttribute.ConstructorArguments[0].Value is true;

        return new AggregateInfo()
        {
            AggregateNamespace = aggregateType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
            AggregateName = aggregateType.Name,
            AggregateFullName = aggregateType.ToDisplayString(),
            ApplyMethods = applyMethods,
            CreateMethods = createMethods,
            RepositoryNamespace = repositoryType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
            RepositoryName = repositoryType.Name,
            RepositoryFullName = repositoryType.ToDisplayString(),
            StateRepositoryName = $"{aggregateType.Name}StateRepository",
            StateRepositoryNamespace = $"{repositoryType.ContainingNamespace.ToDisplayString().Replace("<global namespace>","")}",
            StateRepositoryFullName = $"{repositoryType.ContainingNamespace.ToDisplayString().Replace("<global namespace>","")}.{aggregateType.Name}StateRepository",
            CreateStateRepository = createStateRepository
        };
    }
    
    private static EventInfo[] CreateEventInfos(AggregateInfo info)
    {
        var events = new List<EventInfo>(); ;
        foreach (var createMethod in info.CreateMethods)
        {
            events.Add(new EventInfo
            {
                EventName = createMethod.EventName,
                EventFullName = createMethod.EventFullName,
                EventNamespace = createMethod.EventNamespace,
                EventKebabCaseName = ToKebabCase(createMethod.EventName),
                AggregateFullName = createMethod.AggregateFullName,
                IsCreateEvent = true
            });
        }
        foreach (var applyMethod in info.ApplyMethods)
        {
            events.Add(new EventInfo
            {
                EventName = applyMethod.EventName,
                EventFullName = applyMethod.EventFullName,
                EventNamespace = applyMethod.EventNamespace,
                EventKebabCaseName = ToKebabCase(applyMethod.EventName, false),
                AggregateFullName = applyMethod.AggregateFullName,
                IsCreateEvent = false
            });
        }

        return events.ToArray();
    }

    private static string CreateProjectionSource(AggregateInfo info, EventInfo eventInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.AggregateNamespace))
            namespaces.Add(info.AggregateNamespace);
        if (!string.IsNullOrWhiteSpace(eventInfo.EventNamespace))
            namespaces.Add(eventInfo.EventNamespace);
        foreach (var ns in namespaces.Distinct())
            sb.AppendLine($"using {ns};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {info.AggregateName}{eventInfo.EventName}Projection : AbstractProjection<{info.AggregateName}, {eventInfo.EventName}>");
        sb.AppendLine("{");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string CreateProjectorSource(AggregateInfo info, ImmutableArray<EventInfo> eventInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");
        sb.AppendLine("using FluentResults;");
        

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.AggregateNamespace))
            namespaces.Add(info.AggregateNamespace);
        namespaces.AddRange(eventInfos.Where(x => !string.IsNullOrWhiteSpace(x.EventNamespace)).Select(x => x.EventNamespace));
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {info.AggregateName}Projector : IProjector<{info.AggregateName}>");
        sb.AppendLine("{");
        
        var projectionFieldNames = eventInfos.Select(eventInfo => new { Key = eventInfo, FieldName = $"_{char.ToLower(info.AggregateName[0]) + info.AggregateName.Substring(1)}{eventInfo.EventName}Projection"}).ToDictionary(arg => arg.Key, arg => arg.FieldName);
        var projectionParameterNames = eventInfos.Select(eventInfo => new { Key = eventInfo, FieldName = $"{char.ToLower(info.AggregateName[0]) + info.AggregateName.Substring(1)}{eventInfo.EventName}Projection"}).ToDictionary(arg => arg.Key, arg => arg.FieldName);
        
        foreach (var eventInfo in eventInfos)
        {
            sb.AppendLine($"    private readonly {info.AggregateName}{eventInfo.EventName}Projection {projectionFieldNames[eventInfo]};");
        }
        
        sb.AppendLine();
        sb.AppendLine($"    public {info.AggregateName}Projector(");
        foreach (var eventInfo in eventInfos)
        {
            sb.Append($"        {info.AggregateName}{eventInfo.EventName}Projection {projectionParameterNames[eventInfo]}");
            if (eventInfo != eventInfos.Last())
                sb.AppendLine(",");
            else 
                sb.AppendLine(")");
        }
        sb.AppendLine("    {");
        foreach (var eventInfo in eventInfos)
        {
            sb.AppendLine($"        {projectionFieldNames[eventInfo]} = {projectionParameterNames[eventInfo]};");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<Result> ProjectAsync({info.AggregateName} state, IEvent @event, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return @event.GetType() switch");
        sb.AppendLine("        {");
        foreach (var eventInfo in eventInfos)
        {
            sb.AppendLine($"            {{ }} type when type == typeof({eventInfo.EventName}) => await Result.Try(() => {projectionFieldNames[eventInfo]}.ProjectAsync(state, ({eventInfo.EventName})@event, cancellationToken)),");
        }
        sb.AppendLine("            _ => Result.Fail(\"No projection found for event type \" + @event.GetType().Name)");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    
    private static string CreateStateProjectorSource(AggregateInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");
        sb.AppendLine("using EventSourcing.Stores;");
        sb.AppendLine("using EventSourcing.Mappers;");
        sb.AppendLine("using FluentResults;");
        

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.AggregateNamespace))
            namespaces.Add(info.AggregateNamespace);
        foreach (var eventNamespace in namespaces.Distinct())
            sb.AppendLine($"using {eventNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {info.AggregateName}StateProjector : IProjector<{info.AggregateName}>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IStateStore _stateStore;");
        sb.AppendLine("    private readonly ISerializationRegistry<" + info.AggregateName + "> _serializationRegistry;");
        sb.AppendLine();
        sb.AppendLine($"    public {info.AggregateName}StateProjector(IStateStore stateStore, ISerializationRegistry<{info.AggregateName}> serializationRegistry)");
        sb.AppendLine("    {");
        sb.AppendLine("        _stateStore = stateStore;");
        sb.AppendLine("        _serializationRegistry = serializationRegistry;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<Result> ProjectAsync({info.AggregateName} state, IEvent @event, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var serializeResult = _serializationRegistry.Serialize(state);");
        sb.AppendLine("        if (serializeResult.IsFailed)");
        sb.AppendLine($"            return new Error(\"Failed to serialize aggregate of type {info.AggregateName}\").CausedBy(serializeResult.Errors);");
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
    
    private static string CreateProjectorsDependencyInjectionSource(ImmutableArray<AggregateInfo> aggregateInfos, ImmutableArray<EventInfo> allEventInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Projections;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        
        var namespaces = new List<string>();
        namespaces.AddRange(aggregateInfos.Where(x => !string.IsNullOrWhiteSpace(x.AggregateNamespace)).Select(x => x.AggregateNamespace));
        namespaces.AddRange(aggregateInfos.Where(x => !string.IsNullOrWhiteSpace(x.RepositoryNamespace)).Select(x => x.RepositoryNamespace));
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
        
        foreach (var info in aggregateInfos)
            sb.AppendLine($"    /// <item>IProjector&lt;{info.AggregateName}&gt; (Implementation: <see cref=\"{info.AggregateName}Projector\"/>)</item>");
        foreach (var info in aggregateInfos.Where(x => x.CreateStateRepository))
            sb.AppendLine($"    /// <item>IProjector&lt;{info.AggregateName}&gt; (Implementation: <see cref=\"{info.AggregateName}StateProjector\"/>)</item>");
        
        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void AddProjectors(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var info in aggregateInfos)
            sb.AppendLine($"        services.Add{info.AggregateName}Projector();");
        foreach (var info in aggregateInfos.Where(x => x.CreateStateRepository))
            sb.AppendLine($"        services.Add{info.AggregateName}StateProjector();");

        sb.AppendLine("    }");
        
        foreach (var info in aggregateInfos) 
        {
            sb.AppendLine();    
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the {info.AggregateName} projector in the service collection.</para>");
            sb.AppendLine($"    /// <para>In order to register all projectors use the <see cref=\"AddProjectors\"/> method.</para>");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static IServiceCollection Add{info.AggregateName}Projector(this IServiceCollection services)");
            sb.AppendLine("    {");
            // Register all the projections for the projector
            foreach (var eventInfo in allEventInfos.Where(e => e.AggregateFullName == info.AggregateFullName))
            {
                var projectionName = $"{info.AggregateName}{eventInfo.EventName}Projection";
                sb.AppendLine($"        services.AddScoped<{projectionName}>();");
            }
            sb.AppendLine($"        services.AddScoped<IProjector<{info.AggregateName}>, {info.AggregateName}Projector>();");
            sb.AppendLine($"        return services;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        foreach (var info in aggregateInfos.Where(x => x.CreateStateRepository)) 
        {
            sb.AppendLine();    
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the <see cref=\"{info.AggregateName}StateProjector\"/> in the service collection.</para>");
            sb.AppendLine($"    /// <para>In order to register all projectors use the <see cref=\"AddProjectors\"/> method.</para>");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static IServiceCollection Add{info.AggregateName}StateProjector(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine($"        services.AddScoped<IProjector<{info.AggregateName}>, {info.AggregateName}StateProjector>();");
            sb.AppendLine("        return services;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        
        return sb.ToString();
    }

    
    private static string ToKebabCase(string type, bool withVersion = true)
    {
        var kebabCaseName = string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        // Check if the kebab case name already has a version number with a regex
        if (!_versionSuffixRegex.IsMatch(kebabCaseName) && withVersion)
            kebabCaseName += "-v1"; // default versioning
        return kebabCaseName;
    }
    
    private class AggregateInfo
    {
        public string AggregateNamespace = "";
        public string AggregateName = "";
        public string AggregateFullName = "";
        public List<MutateMethodInfo> ApplyMethods = new();
        public List<MutateMethodInfo> CreateMethods = new();
        public string RepositoryNamespace = "";
        public string RepositoryName = "";
        public string RepositoryFullName = "";
        public string StateRepositoryName = "";
        public string StateRepositoryNamespace = "";
        public string StateRepositoryFullName = "";
        public bool CreateStateRepository = false;
    }

    private class MutateMethodInfo
    {
        public string MethodName = "";
        public string MethodFullName = "";
        public string AggregateFullName = "";
        public string EventNamespace = "";
        public string EventName = "";
        public string EventFullName = "";
        public string ReturnType = "";
    }

    private class EventInfo
    {
        public string EventName = "";
        public string EventFullName = "";
        public string EventNamespace = "";
        public string EventKebabCaseName = "";
        public string AggregateFullName = "";
        public bool IsCreateEvent = false;
    }
}