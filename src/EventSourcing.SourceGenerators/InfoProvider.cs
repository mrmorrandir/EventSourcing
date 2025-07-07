using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventSourcing.SourceGenerators;

public static class InfoProvider
{
    private static readonly Regex _versionSuffixRegex = new(@"-v[0-9]+$");

    public class EventSourcingInfo
    {
        public Aggregate Aggregate { get; set; } = new();
        public Repository Repository { get; set; } = new();
        public StateRepository StateRepository { get; set; } = new();
        public List<EventMapper> EventMappers { get; set; } = [];
        public Projector Projector { get; set; } = new();
        public StateProjector StateProjector { get; set; } = new();
        
        public List<Projection> Projections { get; set; } = [];
        
        public override string ToString() => $"Aggregate: {Aggregate}\n" +
                                             $"Repository: {Repository}\n" +
                                             $"StateRepository: {StateRepository}\n" +
                                             $"EventMappers:\n{string.Join("\n", EventMappers.Select(em => $"- {em}"))}";
    }

    public class Aggregate
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public List<Method> ApplyMethods { get; set; } = [];
        public List<Method> CreateMethods { get; set; } = [];
        public List<Event> ApplyEvents { get; set; } = [];
        public List<Event> CreateEvents { get; set; } = [];
        
        public List<Event> Events { get; set; } = [];

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n" +
                                             $"ApplyMethods:\n{string.Join("\n", ApplyMethods.Select(am => $"  - {am}"))}\n" +
                                             $"CreateMethods:\n{string.Join("\n", CreateMethods.Select(cm => $"  - {cm}"))}\n" +
                                             $"ApplyEvents:\n{string.Join("\n", ApplyEvents.Select(e => $"  - {e}"))}\n" +
                                             $"CreateEvents:\n{string.Join("\n", CreateEvents.Select(e => $"  - {e}"))}\n";
    }

    public class Repository
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNamespaceForFiles => $"{Namespace.Replace("global::", "")}";
        public string SaveFullNameForFiles => $"{SaveNamespaceForFiles}.{SaveNameForFiles}";

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n";
    }

    public class StateRepository
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNamespaceForFiles => $"{Namespace.Replace("global::", "")}";
        public string SaveFullNameForFiles => $"{SaveNamespaceForFiles}.{SaveNameForFiles}";
        public bool Create { get; set; }

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n" +
                                             $"Create: {Create}";
    }

    public class Method
    {
        public string MethodName { get; set; } = string.Empty;
        public string MethodFullName { get; set; } = string.Empty;
        public string ParameterNamespace { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string ParameterFullName { get; set; } = string.Empty;

        public override string ToString() => $"MethodName: {MethodName}\n" +
                                             $"MethodFullName: {MethodFullName}\n" +
                                             $"ParameterNamespace: {ParameterNamespace}\n" +
                                             $"ParameterName: {ParameterName}\n" +
                                             $"ParameterFullName: {ParameterFullName}\n";
    }

    public class EventMapper
    {
        public Mapper Mapper { get; set; } = new();
        public Event Event { get; set; } = new();

        public override string ToString() => $"Mapper: {Mapper}\nEvent: {Event}\n";
    }

    public class Mapper
    {
        public string Name { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string VariableName => $"{char.ToLower(SaveNameForCode[0]) + SaveNameForCode.Substring(1)}";
        public string FieldName => $"_{VariableName}";
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {Fullname}\n" +
                                             $"VariableName: {VariableName}\n" +
                                             $"FieldName: {FieldName}\n";
    }

    public class Event
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public string VariableName => $"{char.ToLower(SaveNameForCode[0]) + SaveNameForCode.Substring(1)}";
        public string FieldName => $"_{VariableName}";
        
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n" +
                                             $"SchemaName: {SchemaName}\n" +
                                             $"VariableName: {VariableName}\n" +
                                             $"FieldName: {FieldName}\n";
    }
    
    public class Projector
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNamespaceForFiles => $"{Namespace.Replace("global::", "")}";
        public string SaveFullNameForFiles => $"{SaveNamespaceForFiles}.{SaveNameForFiles}";
        public string VariableName => $"{char.ToLower(SaveNameForCode[0]) + SaveNameForCode.Substring(1)}";
        public string FieldName => $"_{VariableName}";

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n";
    }
    
    public class StateProjector
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNamespaceForFiles => $"{Namespace.Replace("global::", "")}";
        public string SaveFullNameForFiles => $"{SaveNamespaceForFiles}.{SaveNameForFiles}";
        public string VariableName => $"{char.ToLower(SaveNameForCode[0]) + SaveNameForCode.Substring(1)}";
        public string FieldName => $"_{VariableName}";
        
        public bool Create { get; set; }

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n";
    }

    public class Projection
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SaveNameForFiles => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNameForCode => $"{Name.Replace("<", "Of").Replace(">", "")}";
        public string SaveNamespaceForFiles => $"{Namespace.Replace("global::", "")}";
        public string SaveFullNameForFiles => $"{SaveNamespaceForFiles}.{SaveNameForFiles}";
        public string VariableName => $"{char.ToLower(SaveNameForCode[0]) + SaveNameForCode.Substring(1)}";
        public string FieldName => $"_{VariableName}";
        public Event Event { get; set; }

        public override string ToString() => $"Namespace: {Namespace}\n" +
                                             $"Name: {Name}\n" +
                                             $"FullName: {FullName}\n";
    }

    public static bool IsRepositoryCandidate(SyntaxNode node)
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

    public static EventSourcingInfo? GetEventSourcingInfo(GeneratorSyntaxContext context)
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

        // Analyze aggregateType and it's baseTypes hierarchie for Apply methods
        Method SelectMethod(IMethodSymbol m)
        {
            var eventType = m.Parameters[0].Type;
            return new Method
            {
                ParameterNamespace = eventType.ContainingNamespace.ToDisplayString().Replace("<global namespace>", ""),
                ParameterName = eventType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                ParameterFullName = eventType.ToDisplayString()
            };
        }

        var applyMethods = new List<Method>();
        var currentBaseType = aggregateType;
        while (currentBaseType != null)
        {
            var baseApplyMethods = currentBaseType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m is { Name: "Apply", IsStatic: false, Parameters.Length: 1 })
                .Select(SelectMethod)
                .ToList();

            applyMethods.AddRange(baseApplyMethods);
            currentBaseType = currentBaseType.BaseType;
        }

        // Analyze aggregateType and it's baseTypes hierarchie for Create methods
        var createMethods = new List<Method>();
        currentBaseType = aggregateType;
        while (currentBaseType != null)
        {
            var baseApplyMethods = currentBaseType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m is { Name: "Create", IsStatic: true, Parameters.Length: 1 })
                .Select(SelectMethod)
                .ToList();

            createMethods.AddRange(baseApplyMethods);
            currentBaseType = currentBaseType.BaseType;
        }

        if (applyMethods.Count == 0 && createMethods.Count == 0)
            return null;

        // Check if the Repository has a [UseStateRepository] attribute
        var createStateRepository = false;
        var useStateRepositoryAttribute = repositoryType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "EventSourcing.Repositories.UseStateRepositoryAttribute");
        if (useStateRepositoryAttribute != null)
            createStateRepository = useStateRepositoryAttribute.ConstructorArguments.Length > 0 && useStateRepositoryAttribute.ConstructorArguments[0].Value is true;

        // Create AggregateInfo data / names / namespaces
        var aggregateNamespace = aggregateType.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "");
        var aggregateName = aggregateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var aggregateFullName = aggregateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var repositoryNamespace = repositoryType.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "");
        var repositoryName = repositoryType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var repositoryFullName = repositoryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var stateRepositoryName = $"{aggregateType.Name}StateRepository";
        var stateRepositoryFullName = $"{repositoryNamespace}.{stateRepositoryName}";

        // Create AggregateInfo object
        var sourceGenInfo = new EventSourcingInfo
        {
            Aggregate = new Aggregate
            {
                Namespace = aggregateNamespace,
                Name = aggregateName,
                FullName = aggregateFullName,
                ApplyMethods = applyMethods,
                CreateMethods = createMethods
            },
            Repository = new Repository
            {
                Namespace = repositoryNamespace,
                Name = repositoryName,
                FullName = repositoryFullName
            },
            StateRepository = new StateRepository
            {
                Namespace = repositoryNamespace,
                Name = stateRepositoryName,
                FullName = stateRepositoryFullName,
                Create = createStateRepository
            }
        };

        // Create Events from Parameters
        EventMapper SelectEventMapper(Method method)
        {
            var eventNameEscaped = method.ParameterName.Replace("<", "Of").Replace(">", "");

            var mapperName = $"{sourceGenInfo.Aggregate.Name}{eventNameEscaped}Mapper";
            var mapperFullName = $"{sourceGenInfo.Repository.Namespace}.{mapperName}";
            var mapperNamespace = sourceGenInfo.Repository.Namespace;

            return new EventMapper
            {
                Event = new Event
                {
                    Name = method.ParameterName,
                    FullName = method.ParameterFullName,
                    Namespace = method.ParameterNamespace,
                    SchemaName = $"{sourceGenInfo.Aggregate.SaveNameForCode.ToLower()}-{ToKebabCase(eventNameEscaped)}",
                },
                Mapper = new Mapper
                {
                    Name = mapperName,
                    Fullname = mapperFullName,
                    Namespace = mapperNamespace
                }
            };
        }

        var createEventMapperInfos = createMethods
            .GroupBy(method => method.ParameterFullName)
            .Select(group => group.First())
            .Select(SelectEventMapper).ToList();

        var applyEventMapperInfos = applyMethods
            .GroupBy(method => method.ParameterFullName)
            .Select(group => group.First())
            .Select(SelectEventMapper).ToList();

        sourceGenInfo.Aggregate.CreateEvents = createEventMapperInfos
            .Select(info => info.Event)
            .ToList();
        sourceGenInfo.Aggregate.ApplyEvents = applyEventMapperInfos
            .Select(info => info.Event)
            .ToList();
        sourceGenInfo.Aggregate.Events = sourceGenInfo.Aggregate.CreateEvents.Concat(sourceGenInfo.Aggregate.ApplyEvents).ToList();
        sourceGenInfo.EventMappers = createEventMapperInfos.Concat(applyEventMapperInfos).ToList();
        
        sourceGenInfo.Projector = new Projector()
        {
            Namespace = $"{sourceGenInfo.Repository.Namespace}",
            Name = $"{sourceGenInfo.Aggregate.SaveNameForCode}Projector",
            FullName = $"{sourceGenInfo.Repository.Namespace}.{sourceGenInfo.Aggregate.SaveNameForCode}Projector"
        };
        
        sourceGenInfo.StateProjector = new StateProjector()
        {
            Namespace = $"{sourceGenInfo.Repository.Namespace}",
            Name = $"{sourceGenInfo.Aggregate.SaveNameForCode}StateProjector",
            FullName = $"{sourceGenInfo.Repository.Namespace}.{sourceGenInfo.Aggregate.SaveNameForCode}StateProjector",
            Create = createStateRepository
        };
        
        Projection SelectProjection(EventMapper eventMapper)
        {
            var projectionNamespace = $"{sourceGenInfo.Repository.Namespace}";
            var projectionName = $"{sourceGenInfo.Aggregate.SaveNameForCode}{eventMapper.Event.SaveNameForCode}Projection";
            var projectionFullName = $"{projectionNamespace}.{projectionName}";

            return new Projection()
            {
                Namespace = projectionNamespace,
                Name = projectionName,
                FullName = projectionFullName,
                Event = eventMapper.Event
            };
        }
        
        sourceGenInfo.Projections = sourceGenInfo.EventMappers
            .Select(SelectProjection)
            .ToList();

        return sourceGenInfo;
    }

    private static string ToSaveName(string type)
    {
        // extract "<" and ">" and everything in between
        var match = Regex.Match(type, @"<(.+?)>");
        if (match.Success)
            // If the type is generic, return the name without the generic parameters
            return type.Replace($"<{match.Groups[1].Value}>", "");

        // If the type is not generic, return the name as is
        return type;
    }

    private static string ToKebabCase(string type, bool withVersion = true)
    {
        var kebabCaseName = string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        // Check if the kebab case name already has a version number with a regex
        if (!_versionSuffixRegex.IsMatch(kebabCaseName) && withVersion)
            kebabCaseName += "-v1"; // default versioning
        return kebabCaseName;
    }
}