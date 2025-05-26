
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventSourcing.SourceGenerators;

[Generator]
public partial class AggregateFullRepositoryGenerator : IIncrementalGenerator
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
                (ctx, _) => GetAggregateInfo(ctx))
            .Where(info => info is not null);

        context.RegisterSourceOutput(aggregates, (spc, info) =>
        {
            var mapperInfos = GetMapperInfo(info!);
            if (mapperInfos.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("ESG001", "No mappers found", "No mappers found for aggregate {0}", "EventSourcing", DiagnosticSeverity.Warning, true),
                    Location.None, info!.AggregateName));
                return;
            }
            foreach (var mapperInfo in mapperInfos)
            {
                var mapperSource = CreateMapper(info!, mapperInfo);
                spc.AddSource($"{mapperInfo.MapperNamespace}.{mapperInfo.MapperName}.g.cs", SourceText.From(mapperSource, Encoding.UTF8));
            }
            
            // Generate the repository source code
            var source = GenerateRepository(info!, mapperInfos);
            spc.AddSource($"{info!.RepositoryNamespace}.{info!.AggregateName}Repository.g.cs", SourceText.From(source, Encoding.UTF8));
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
            .Any(t => t.Type.ToString().StartsWith("IAggregateRepository<"));
    }

    private static AggregateInfo? GetAggregateInfo(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = ModelExtensions.GetDeclaredSymbol(model, classSyntax) as INamedTypeSymbol;
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
            .Select(m => {
                var eventType = m.Parameters[0].Type;
                return new MutateMethodInfo
                {
                    EventNamespace = eventType.ContainingNamespace.ToDisplayString(),
                    EventName = eventType.Name,
                    EventFullName = eventType.ToDisplayString(),
                    ReturnType = m.ReturnType.ToDisplayString()
                };
            })
            .ToList();
        
        var createMethods = aggregateType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == "Create" && m.IsStatic && m.Parameters.Length == 1)
            .Select(m => {
                var eventType = m.Parameters[0].Type;
                return new MutateMethodInfo
                {
                    EventNamespace = eventType.ContainingNamespace.ToDisplayString(),
                    EventName = eventType.Name,
                    EventFullName = eventType.ToDisplayString(),
                    ReturnType = m.ReturnType.ToDisplayString()
                };
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
            RepositoryNamespace = repoNamespace
        };
    }

    private static string GenerateRepository(AggregateInfo info, MapperInfo[] mapperInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using EventSourcing;");
        sb.AppendLine("using EventSourcing.Mappers;");
        sb.AppendLine("using EventSourcing.Stores;");
        sb.AppendLine("using EventSourcing.Repositories;");

        var eventNamespaces = info.ApplyMethods.Select(x => x.EventNamespace).ToList().Concat(info.CreateMethods.Select(x => x.EventNamespace)).ToList();
        eventNamespaces.Add(info.Namespace);
        eventNamespaces = eventNamespaces.Distinct().ToList();
        foreach(var eventNamespace in eventNamespaces)
            sb.AppendLine($"using {eventNamespace};");
        
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {info.AggregateName}Repository");
        sb.AppendLine("{");
        
        // Create static mapper fields
        foreach(var mapperInfo in mapperInfos)
        {
            var mapperFieldName = $"_{char.ToLower(mapperInfo.EventFullName[0]) + mapperInfo.EventFullName[1..].Replace(".", "")}";
            if (!mapperFieldName.EndsWith("Mapper"))
                mapperFieldName += "Mapper";
            
            sb.AppendLine($"    private static readonly {mapperInfo.MapperFullname} {mapperFieldName} = new();");
        }

        sb.AppendLine("    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();");
        sb.AppendLine();
        sb.AppendLine("    private readonly Dictionary<Guid, List<object>> _streams = new();");
        // sb.AppendLine("    private readonly IEventStore _eventStore;");
        sb.AppendLine();
        
        // Create static constructor to initialize mappers
        sb.AppendLine($"    static {info.AggregateName}Repository()");
        sb.AppendLine("    {");
        foreach (var mapperInfo in mapperInfos)
        {
            sb.AppendLine($"        foreach (string schema in {mapperInfo.MapperFieldName}.Schemas)");
            sb.AppendLine($"            _deserializers.Add(schema, (typeSchema, data) => {mapperInfo.MapperFieldName}.Deserialize(typeSchema, data));");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
        // sb.AppendLine($"    public {info.AggregateName}Repository(IEventStore eventStore)");
        // sb.AppendLine("    {");
        // sb.AppendLine("        _eventStore = eventStore;");
        // sb.AppendLine("    }");
        sb.AppendLine($"    public {info.AggregateName}Repository()");
        sb.AppendLine("    {");
        sb.AppendLine();
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public {info.AggregateName} Get(Guid id)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!_streams.TryGetValue(id, out var events))");
        sb.AppendLine("            throw new InvalidOperationException($\"Aggregate with ID {id} not found.\");");
        sb.AppendLine();
        sb.AppendLine($"        {info.AggregateName}? aggregate = null;");
        sb.AppendLine("        foreach (var evt in events)");
        sb.AppendLine("            aggregate = aggregate == null ? CreateFromEvent(evt) : ApplyEvent(aggregate, evt);");
        sb.AppendLine();
        sb.AppendLine("        return aggregate ?? throw new InvalidOperationException($\"No events found for aggregate with ID {id}.\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void Save(Guid id, IEnumerable<IEvent> events)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!_streams.ContainsKey(id))");
        sb.AppendLine("            _streams[id] = [];");
        sb.AppendLine("        foreach (var evt in events)");
        sb.AppendLine("            _streams[id].Add(evt);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public {info.AggregateName} SaveAndGet(Guid id, IEnumerable<IEvent> events)");
        sb.AppendLine("    {");
        sb.AppendLine("        Save(id, events);");
        sb.AppendLine("        return Get(id);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Create ApplyEvent method
        if (info.ApplyMethods.Count > 0)
        {
            sb.AppendLine($"    private static {info.AggregateName} ApplyEvent({info.AggregateName} aggregate, object evt)");
            sb.AppendLine("    {");
            sb.AppendLine("        return evt switch");
            sb.AppendLine("        {");
            
            // Create cases for each event type / each apply method
            foreach (var method in info.ApplyMethods) 
                sb.AppendLine($"            {method.EventName} e => aggregate.Apply(e),");
            
            sb.AppendLine("            _ => throw new InvalidOperationException($\"Unknown event type: {evt.GetType().Name}\")");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
        }

        // Create CreateFromEvent method
        if (info.CreateMethods.Count > 0)
        {
            sb.AppendLine($"    private static {info.AggregateName} CreateFromEvent(object evt)");
            sb.AppendLine("    {");
            sb.AppendLine("        return evt switch");
            sb.AppendLine("        {");
            
            // Create cases for each event type / each create method
            foreach (var method in info.CreateMethods) 
                sb.AppendLine($"            {method.EventName} e => {info.AggregateName}.Create(e),");
            
            sb.AppendLine("            _ => throw new InvalidOperationException($\"Unknown event type: {evt.GetType().Name}\")");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
        }
        
        sb.AppendLine();
        sb.AppendLine("    private static ISerializedEvent Serialize(IEvent @event)");
        sb.AppendLine("    {");
        sb.AppendLine("        return @event.GetType() switch");
        sb.AppendLine("        {");
        
        // Create serialization cases for each event type
        foreach (var mapperInfo in mapperInfos)
            sb.AppendLine($"            {{ }} type when type == typeof({mapperInfo.EventFullName}) => {mapperInfo.MapperFieldName}.Serialize(({mapperInfo.EventFullName})@event),");
        
        sb.AppendLine("            _ => throw new EventRegistryException($\"No serializer found for type {@event.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static IEvent Deserialize(string schema, string data)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!_deserializers.TryGetValue(schema, out var deserializer))");
        sb.AppendLine("            throw new EventRegistryException($\"No deserializer found for type {schema}\");");
        sb.AppendLine();
        sb.AppendLine("        return deserializer(schema, data);");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private static MapperInfo[] GetMapperInfo(AggregateInfo info)
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
                MapperName = $"{evt.EventName}Mapper",
                MapperFullname = $"{info.RepositoryNamespace}.{evt.EventName}Mapper",
                MapperNamespace = info.RepositoryNamespace,
            });
        }

        return mapperDataList.ToArray();
    }

    private static string CreateMapper(AggregateInfo info, MapperInfo evt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using EventSourcing.Mappers;");
        sb.AppendLine();
        sb.AppendLine($"using {evt.EventNamespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.RepositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {evt.EventName}Mapper : AbstractEventMapper<{evt.EventName}>");
        sb.AppendLine( "{");
        sb.AppendLine($"    public {evt.EventName}Mapper()");
        sb.AppendLine( "    {");
        sb.AppendLine($"        WillSerialize(\"{evt.EventKebabCaseName}\");");
        sb.AppendLine($"        CanDeserialize(\"{evt.EventKebabCaseName}\");");
        sb.AppendLine($"        Configure();");
        sb.AppendLine( "    }");
        sb.AppendLine();
        sb.AppendLine($"    partial void Configure();");
        sb.AppendLine( "}");

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
        public string AggregateName = "";
        public List<MutateMethodInfo> ApplyMethods = new();
        public List<MutateMethodInfo> CreateMethods = new();
        public string Namespace = "";
        public string RepositoryNamespace = "";
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
        public string MapperFieldName => $"_{char.ToLower(EventFullName[0]) + EventFullName[1..].Replace(".", "")}Mapper";
    }
}