using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators.Serialization;

[Generator]
public partial class SerializationGenerator : IIncrementalGenerator
{
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*-v[0-9]+$")]
    private static partial Regex TypeRegex();

    [GeneratedRegex(@"-v[0-9]+$")]
    private static partial Regex VersionSuffixRegex();
    
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

            var allMapperInfos = new List<MapperInfo>();
            foreach (var info in infos)
            {
                var mapperInfos = CreateMapperInfos(info!);
                if (mapperInfos.Length == 0)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ESG001", $"Aggregate has no 'Create' or 'Apply' methods.", "The aggregate {0} has no 'Create' or 'Apply' methods (with event as parameter) to create mappers for.", "EventSourcing", DiagnosticSeverity.Warning, true), Location.None, info!.AggregateName));
                    return;
                }
                allMapperInfos.AddRange(mapperInfos);

                foreach (var mapperInfo in mapperInfos)
                {
                    var mapperSource = CreateMapperSource(info!, mapperInfo);
                    spc.AddSource($"{mapperInfo.MapperNamespace}.{mapperInfo.MapperName}.g.cs", SourceText.From(mapperSource, Encoding.UTF8));
                }

                // Create serialization registry
                var serializationRegistrySource = CreateSerializationRegistrySource(info!, [..mapperInfos]);
                spc.AddSource($"{info!.RepositoryNamespace}.{info!.AggregateName}SerializationRegistry.g.cs", SourceText.From(serializationRegistrySource, Encoding.UTF8));

                // Create the dependency injection for the serialization registry
                var serializationRegistrationSource = CreateSerializationDependencyInjectionSource(info!, [..mapperInfos]);
                spc.AddSource($"{info!.RepositoryNamespace}.{info!.AggregateName}SerializationDependencyInjection.g.cs", SourceText.From(serializationRegistrationSource, Encoding.UTF8));
            }
            
            var completeDependencyInjectionSource = CreateCompleteSerializationDependencyInjectionSource(infos!,[..allMapperInfos]);
            spc.AddSource($"SerializationDependencyInjection.g.cs", SourceText.From(completeDependencyInjectionSource, Encoding.UTF8));
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
                    EventNamespace = eventType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
                    EventName = eventType.Name,
                    EventFullName = eventType.ToDisplayString(),
                    ReturnType = m.ReturnType.ToDisplayString()
                };
            })
            .ToList();

        if (applyMethods.Count == 0 && createMethods.Count == 0)
            return null;

        return new AggregateInfo
        {
            AggregateNamespace = aggregateType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
            AggregateName = aggregateType.Name,
            AggregateFullName = aggregateType.ToDisplayString(),
            ApplyMethods = applyMethods,
            CreateMethods = createMethods,
            RepositoryNamespace = repositoryType.ContainingNamespace.ToDisplayString().Replace("<global namespace>",""),
            RepositoryName = repositoryType.Name,
            RepositoryFullName = repositoryType.ToDisplayString()
        };
    }
    
    private static MapperInfo[] CreateMapperInfos(AggregateInfo info)
    {
        var events = info.CreateMethods.Concat(info.ApplyMethods).DistinctBy(x => x.EventFullName).ToList();
        if (events.Count == 0)
            return [];

        var mapperDataList = new List<MapperInfo>();
        foreach (var evt in events)
        {
            mapperDataList.Add(new MapperInfo
            {
                EventName = evt.EventName,
                EventFullName = evt.EventFullName,
                EventNamespace = evt.EventNamespace,
                EventKebabCaseName = ToKebabCase(evt.EventName),
                MapperName = $"{info.AggregateName}{evt.EventName}Mapper",
                MapperFullname = $"{info.RepositoryNamespace}.{evt.EventName}Mapper",
                MapperNamespace = info.RepositoryNamespace,
                MapperFieldName = $"_{char.ToLower(evt.EventName[0]) + evt.EventName[1..]}Mapper"
            });
        }

        return mapperDataList.ToArray();
    }

    private static string CreateMapperSource(AggregateInfo aggregateInfo, MapperInfo mapperInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using EventSourcing.Mappers;");
        
        if (!string.IsNullOrWhiteSpace(mapperInfo.EventNamespace))
            sb.AppendLine($"using {mapperInfo.EventNamespace};");
        
        sb.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(aggregateInfo.RepositoryNamespace))
            sb.AppendLine($"namespace {aggregateInfo.RepositoryNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"public partial class {mapperInfo.MapperName} : AbstractEventMapper<{mapperInfo.EventName}>");
        sb.AppendLine( "{");
        sb.AppendLine($"    public {mapperInfo.MapperName}()");
        sb.AppendLine( "    {");
        sb.AppendLine($"        WillSerialize(\"{mapperInfo.EventKebabCaseName}\");");
        sb.AppendLine($"        CanDeserialize(\"{mapperInfo.EventKebabCaseName}\");");
        sb.AppendLine($"        Configure();");
        sb.AppendLine( "    }");
        sb.AppendLine();
        sb.AppendLine($"    partial void Configure();");
        sb.AppendLine( "}");

        return sb.ToString();
    }

    private static string CreateSerializationRegistrySource(AggregateInfo aggregateInfo, ImmutableArray<MapperInfo> mapperInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using FluentResults;");
        sb.AppendLine("using EventSourcing.Mappers;");

        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(aggregateInfo.RepositoryNamespace))
            namespaces.Add(aggregateInfo.RepositoryNamespace);
        if (!string.IsNullOrWhiteSpace(aggregateInfo.AggregateNamespace))
            namespaces.Add(aggregateInfo.AggregateNamespace);
        namespaces.AddRange(mapperInfos.Where(x => !string.IsNullOrWhiteSpace(x.MapperNamespace)).Select(x => x.MapperNamespace));
        namespaces.AddRange(mapperInfos.Where(x => !string.IsNullOrWhiteSpace(x.EventNamespace)).Select(x => x.EventNamespace));
        foreach (var ns in namespaces.Distinct())
            sb.AppendLine($"using {ns};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {aggregateInfo.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {aggregateInfo.AggregateName}SerializationRegistry : ISerializationRegistry<{aggregateInfo.AggregateName}>");
        sb.AppendLine("{");
        
        foreach (var mapperInfo in mapperInfos)
            sb.AppendLine($"    private static readonly {mapperInfo.MapperName} {mapperInfo.MapperFieldName} = new();");

        sb.AppendLine("    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();");
        sb.AppendLine();
        sb.AppendLine($"    static {aggregateInfo.AggregateName}SerializationRegistry()");
        sb.AppendLine("    {");
        
        foreach (var mapperInfo in mapperInfos)
        {
            sb.AppendLine($"        foreach (string schema in {mapperInfo.MapperFieldName}.Schemas)");
            sb.AppendLine($"            _deserializers.Add(schema, (typeSchema, data) => {mapperInfo.MapperFieldName}.Deserialize(typeSchema, data));");
        }
        
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public Result<ISerializedEvent> Serialize(IEvent @event)");
        sb.AppendLine("    {");
        sb.AppendLine("        return @event.GetType() switch");
        sb.AppendLine("        {");
        
        // Create serialization cases for each event type
        foreach (var mapperInfo in mapperInfos)
            sb.AppendLine($"            {{ }} type when type == typeof({mapperInfo.EventName}) => Result.Try(() => {mapperInfo.MapperFieldName}.Serialize(({mapperInfo.EventName})@event)),");
        
        sb.AppendLine("            _ => Result.Fail($\"No serializer found for type {@event.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public Result<IEvent> Deserialize(string schema, string data)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!_deserializers.TryGetValue(schema, out var deserializer))");
        sb.AppendLine("            return new Error($\"No deserializer found for type {schema}\");");
        sb.AppendLine();
        sb.AppendLine("        return Result.Try(() => deserializer(schema, data));");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private static string CreateSerializationDependencyInjectionSource(AggregateInfo aggregateInfo, ImmutableArray<MapperInfo> mapperInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using EventSourcing.Mappers;");
        
        var namespaces = new List<string>();
        if (!string.IsNullOrWhiteSpace(aggregateInfo.RepositoryNamespace))
            namespaces.Add(aggregateInfo.RepositoryNamespace);
        if (!string.IsNullOrWhiteSpace(aggregateInfo.AggregateNamespace))
            namespaces.Add(aggregateInfo.AggregateNamespace);
        namespaces.AddRange(mapperInfos.Where(x => !string.IsNullOrWhiteSpace(x.MapperNamespace)).Select(x => x.MapperNamespace));
        namespaces.AddRange(mapperInfos.Where(x => !string.IsNullOrWhiteSpace(x.EventNamespace)).Select(x => x.EventNamespace));
        foreach (var ns in namespaces.Distinct())
            sb.AppendLine($"using {ns};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("public static partial class SerializationDependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// <para>Registers all serialization components for the aggregate {aggregateInfo.AggregateName} in the service collection.</para>");
        sb.AppendLine("    /// <para>Use this method in your infrastructure dependency injection to register all serialization components.</para>");
        sb.AppendLine("    /// <para>In order to register everything for the serialization use the <see cref=\"AddSerialization\"/> method.</para>");
        sb.AppendLine("    /// <para>");
        sb.AppendLine("    /// Serialization components that will be registered:");
        sb.AppendLine("    /// <list type=\"bullet\">");
        sb.AppendLine($"    /// <item><see cref=\"ISerializationRegistry&lt;{aggregateInfo.AggregateName}&gt;\"/></item>");
        foreach (var mapperInfo in mapperInfos)
            sb.AppendLine($"    /// <item><see cref=\"{mapperInfo.MapperName}\"/></item>");
        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static IServiceCollection Add{aggregateInfo.AggregateName}Serialization(this IServiceCollection services)");
        sb.AppendLine("    {");
        sb.AppendLine($"        services.Add{aggregateInfo.AggregateName}Mappers();");
        sb.AppendLine($"        services.Add{aggregateInfo.AggregateName}SerializationRegistry();");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// <para>Registers all mappers for the aggregate in the service collection.</para>");
        sb.AppendLine($"    /// <para>Use this method in your infrastructure dependency injection to register all mappers.</para>");
        sb.AppendLine($"    /// <para>In order to register everything for the serialization use the <see cref=\"Add{aggregateInfo.AggregateName}Serialization\"/> method.</para>");
        sb.AppendLine($"    /// <para>");
        sb.AppendLine($"    /// Mappers that will be registered:");
        sb.AppendLine("    /// <list type=\"bullet\">");
        foreach (var mapperInfo in mapperInfos)
            sb.AppendLine($"    /// <item><see cref=\"{mapperInfo.MapperName}\"/></item>");
        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static IServiceCollection Add{aggregateInfo.AggregateName}Mappers(this IServiceCollection services)");
        sb.AppendLine("    {");
        foreach (var mapperInfo in mapperInfos)
            sb.AppendLine($"        services.Add{mapperInfo.MapperName}();");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// <para>Registers the <see cref=\"ISerializationRegistry&lt;{aggregateInfo.AggregateName}&gt;\"/> in the service collection.</para>");
        sb.AppendLine($"    /// <para>In order to register everything for the serialization use the <see cref=\"Add{aggregateInfo.AggregateName}Serialization\"/> method.</para>");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public static IServiceCollection Add{aggregateInfo.AggregateName}SerializationRegistry(this IServiceCollection services)");
        sb.AppendLine("    {");
        sb.AppendLine($"        services.AddSingleton<ISerializationRegistry<{aggregateInfo.AggregateName}>, {aggregateInfo.AggregateName}SerializationRegistry>();");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Add methods for the single mappers
        foreach (var mapperInfo in mapperInfos)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// <para>Registers the <see cref=\"{mapperInfo.MapperName}\"/> in the service collection.</para>");
            sb.AppendLine($"    /// <para>In order to register everything for the serialization use the <see cref=\"Add{aggregateInfo.AggregateName}Serialization\"/> method.</para>");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static IServiceCollection Add{mapperInfo.MapperName}(this IServiceCollection services)");
            sb.AppendLine("    {");
            sb.AppendLine($"        services.AddSingleton<{mapperInfo.MapperName}>();");
            sb.AppendLine("        return services;");
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    private static string CreateCompleteSerializationDependencyInjectionSource(ImmutableArray<AggregateInfo> aggregateInfos, ImmutableArray<MapperInfo> mapperInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using EventSourcing.Mappers;");
        
        var namespaces = new List<string>();
        namespaces.AddRange(aggregateInfos.Where(x => !string.IsNullOrWhiteSpace(x.AggregateNamespace)).Select(x => x.AggregateNamespace));
        foreach (var ns in namespaces.Distinct())
            sb.AppendLine($"using {ns};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("public static partial class SerializationDependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// <para>Registers all serialization components for all aggregates in the service collection.</para>");
        sb.AppendLine("    /// <para>Use this method in your infrastructure dependency injection to register all serialization components.</para>");
        sb.AppendLine("    /// <para>");
        sb.AppendLine("    /// Serialization components that will be registered:");
        sb.AppendLine("    /// <list type=\"bullet\">");
        foreach (var aggregateInfo in aggregateInfos)
        {
            sb.AppendLine($"    /// <item>ISerializationRegistry&lt;{aggregateInfo.AggregateName}&gt; (Implementation: <see cref=\"{aggregateInfo.AggregateName}SerializationRegistry\"/>)</item>");
            foreach (var mapperInfo in mapperInfos.Where(x => x.MapperNamespace == aggregateInfo.RepositoryNamespace && x.MapperName.StartsWith(aggregateInfo.AggregateName)))
                sb.AppendLine($"    /// <item><see cref=\"{mapperInfo.MapperName}\"/></item>");
        }
        sb.AppendLine("    /// </list>");
        sb.AppendLine("    /// </para>");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static IServiceCollection AddSerialization(this IServiceCollection services)");
        sb.AppendLine("    {");
        foreach (var aggregateInfo in aggregateInfos)
            sb.AppendLine($"        services.Add{aggregateInfo.AggregateName}Serialization();");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    private static string ToKebabCase(string type, bool withVersion = true)
    {
        var kebabCaseName = string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        // Check if the kebab case name already has a version number with a regex
        if (!VersionSuffixRegex().IsMatch(kebabCaseName) && withVersion)
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
    }

    private class MutateMethodInfo
    {
        public string EventNamespace = "";
        public string EventName = "";
        public string EventFullName = "";
        public string ReturnType = "";
    }

    private class MapperInfo
    {
        public string EventName = "";
        public string EventFullName = "";
        public string EventNamespace = "";
        public string EventKebabCaseName = "";
        public string MapperName = "";
        public string MapperFullname = "";
        public string MapperNamespace = "";
        public string MapperFieldName = "";
    }
}