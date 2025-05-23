using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EventSourcing.SourceGenerators;

[Generator]
public class EventRegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all Types that derive from AbstractEventMapper<T>
        var abstractMapperClassDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, ct) => IsAbstractMapperCandidate(s, ct),
                static (ctx, ct) => GetAbstractMapperClassDeclaration(ctx, ct))
            .Where(static x => x is not null)
            .Select((x, ct) => x!);

        var eventClassDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, ct) => IsEventCandidate(s, ct),
                static (ctx, ct) => GetEventTypeDeclaration(ctx, ct))
            .Where(static x => x is not null)
            .Select((x, ct) => x!);

        var compilation = context.CompilationProvider.Combine(abstractMapperClassDeclarations.Collect()).Combine(eventClassDeclarations.Collect());

        context.RegisterSourceOutput(compilation, CreateSource);
    }

    private static bool IsAbstractMapperCandidate(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax &&
               classDeclarationSyntax.BaseList?.Types.Any(baseTypeSyntax => baseTypeSyntax.ToString().Contains("AbstractEventMapper")) == true;
    }

    private static ClassDeclarationSyntax? GetAbstractMapperClassDeclaration(GeneratorSyntaxContext context, CancellationToken cancellationToken) => context.Node as ClassDeclarationSyntax;

    private static bool IsEventCandidate(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is TypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.BaseList?.Types.Any(baseTypeSyntax => baseTypeSyntax.ToString().Contains("IEvent")) == true;
    }

    private static TypeDeclarationSyntax? GetEventTypeDeclaration(GeneratorSyntaxContext context, CancellationToken cancellationToken) => context.Node as TypeDeclarationSyntax;

    private static string GetNamespace(TypeDeclarationSyntax classDeclaration)
    {
        // Traverse up the syntax tree to find the namespace declaration
        var namespaceDeclaration = classDeclaration.Parent;
        while (namespaceDeclaration is not NamespaceDeclarationSyntax && namespaceDeclaration is not FileScopedNamespaceDeclarationSyntax) namespaceDeclaration = namespaceDeclaration?.Parent;

        return namespaceDeclaration switch
        {
            NamespaceDeclarationSyntax namespaceSyntax => namespaceSyntax.Name.ToString(),
            FileScopedNamespaceDeclarationSyntax fileScopedNamespaceSyntax => fileScopedNamespaceSyntax.Name.ToString(),
            _ => string.Empty // Default to empty if no namespace is found
        };
    }
    
    private static IEnumerable<UsingDirectiveSyntax> GetRelevantUsings(SyntaxNode syntaxNode)
    {
        // Get the root of the syntax tree
        var root = syntaxNode.SyntaxTree.GetRoot();

        // Find all using directives in the file
        var usingDirectives = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>();

        return usingDirectives;
    }
    
    private static string? FindNamespaceForType(Compilation compilation, IEnumerable<UsingDirectiveSyntax> usingDirectives, string typeName)
    {
        // Check each namespace from the using directives
        foreach (var usingDirective in usingDirectives)
        {
            var namespaceName = usingDirective.Name.ToString();
            var fullTypeName = $"{namespaceName}.{typeName}";

            // Try to resolve the type in the current namespace
            var typeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
            if (typeSymbol != null)
            {
                return namespaceName; // Return the namespace if the type is found
            }
        }

        return null; // Return null if the type is not found in any namespace
    }

    private static void CreateSource(SourceProductionContext productionContext, ((Compilation compilation, ImmutableArray<ClassDeclarationSyntax> abstractMappersClassDeclarationSyntaxes) abstractMappersTuple, ImmutableArray<TypeDeclarationSyntax> eventClassDeclarationSyntaxes) combinedData)
    {
        var (compilation, abstractMappersClassDeclarationSyntaxes) = combinedData.abstractMappersTuple;
        var events = combinedData.eventClassDeclarationSyntaxes;

        // Extract event class names
        var eventInfos = new List<EventInfo>();
        foreach (var eventClassDeclarationSyntax in events)
        {
            var @namespace = GetNamespace(eventClassDeclarationSyntax);
            var name = $"{eventClassDeclarationSyntax.Identifier.Text}";
            var fullName = $"{@namespace}.{name}";
            var mapperFieldName = $"_{char.ToLower(fullName[0]) + fullName[1..].Replace(".","")}";
            if (!mapperFieldName.EndsWith("Mapper"))
                mapperFieldName += "Mapper";
            eventInfos.Add(new EventInfo(@namespace, fullName, name, mapperFieldName));
        }

        // Extract full class names (namespace + class name)
        var mapperInfos = new List<MapperInfo>();
        foreach (var mapperClassDeclarationSyntax in abstractMappersClassDeclarationSyntaxes)
        {
            var @namespace = GetNamespace(mapperClassDeclarationSyntax);
            var name = $"{mapperClassDeclarationSyntax.Identifier.Text}";
            var fullName = $"{@namespace}.{name}";
            var eventTypeName = mapperClassDeclarationSyntax.BaseList!.Types
                .First(baseTypeSyntax => baseTypeSyntax.ToString().Contains("AbstractEventMapper"))
                .ToString()
                .Split('<')[1]
                .Split('>')[0]
                .Trim();
            
            // get the usings that are used in file of the mapperClassDeclarationSyntax
            var usings = GetRelevantUsings(mapperClassDeclarationSyntax);
            var eventNamespace = FindNamespaceForType(compilation, usings, eventTypeName) ?? @namespace;
            var eventTypeFullName = $"{eventNamespace}.{eventTypeName}";

            // get the name of the event type
            var mapperFieldName = $"_{char.ToLower(fullName[0]) + fullName[1..].Replace(".","")}";
            if (!mapperFieldName.EndsWith("Mapper"))
                mapperFieldName += "Mapper";
            mapperInfos.Add(new MapperInfo(@namespace, fullName, name, eventTypeFullName, eventTypeName, mapperFieldName));
        }

        // Generate the EventRegistry2 class
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Collections.Generic;");
        sourceBuilder.AppendLine("using EventSourcing.Mappers;");
        sourceBuilder.AppendLine("using FluentResults;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace EventSourcing.Generated");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("    /// <summary>");
        sourceBuilder.AppendLine("    /// <para>");
        sourceBuilder.AppendLine("    /// This class is generated by the EventSourcing.SourceGenerators.");
        sourceBuilder.AppendLine("    /// It is a singleton that holds all the mappers and deserializers for the events.");
        sourceBuilder.AppendLine("    /// </para>");
        sourceBuilder.AppendLine("    /// <para>");
        sourceBuilder.AppendLine("    /// AbstractEventMapper&lt;T&gt; implementations:");
        sourceBuilder.AppendLine("    /// <list type=\"bullet\">");
        
        foreach (var mapperInfo in mapperInfos)
            sourceBuilder.AppendLine($"    /// <item><description><see cref=\"{mapperInfo.FullName}\" /> for <see cref=\"{mapperInfo.EventFullName}\">{mapperInfo.EventName}</see></description></item>");
        sourceBuilder.AppendLine("    /// </list>");
        sourceBuilder.AppendLine("    /// </para>");
        sourceBuilder.AppendLine("    /// <para>");
        sourceBuilder.AppendLine("    /// Event classes (covered by DefaultEventMapper&lt;T&gt;):");
        sourceBuilder.AppendLine("    /// <list type=\"bullet\">");
        foreach (var eventInfo in eventInfos)
        {
            if (mapperInfos.Any(x => x.EventFullName == eventInfo.FullName))
                continue;
            sourceBuilder.AppendLine($"    /// <item><description><see cref=\"{eventInfo.FullName}\" /></description></item>");
        }   
        sourceBuilder.AppendLine("    /// </list>");
        sourceBuilder.AppendLine("    /// </para>");
        sourceBuilder.AppendLine("    /// </summary>");
        sourceBuilder.AppendLine("    public class EventRegistry : IEventRegistry");
        sourceBuilder.AppendLine("    {");

        foreach (var mapperInfo in mapperInfos)
            sourceBuilder.AppendLine($"        private readonly {mapperInfo.FullName} {mapperInfo.FieldName} = new();");

        foreach (var eventInfo in eventInfos)
        {
            if (mapperInfos.Any(x => x.EventFullName == eventInfo.FullName))
                continue;
            sourceBuilder.AppendLine($"        private readonly DefaultEventMapper<{eventInfo.FullName}> {eventInfo.FieldName} = new DefaultEventMapper<{eventInfo.FullName}>();");
        }

        sourceBuilder.AppendLine("        private readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public EventRegistry()");
        sourceBuilder.AppendLine("        {");
        foreach (var mapperInfo in mapperInfos)
        {
            sourceBuilder.AppendLine($"            foreach (string schema in {mapperInfo.FieldName}.Types)");
            sourceBuilder.AppendLine($"                _deserializers.Add(schema, (typeSchema, data) => {mapperInfo.FieldName}.Deserialize(typeSchema, data));");
        }
        
        foreach (var eventInfo in eventInfos)
        {
            if (mapperInfos.Any(x => x.EventFullName == eventInfo.FullName))
                continue;
            sourceBuilder.AppendLine($"            foreach (string schema in {eventInfo.FieldName}.Types)");
            sourceBuilder.AppendLine($"                _deserializers.Add(schema, (typeSchema, data) => {eventInfo.FieldName}.Deserialize(typeSchema, data));");
        }

        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public ISerializedEvent Serialize(IEvent @event)");
        sourceBuilder.AppendLine("        {");
        sourceBuilder.AppendLine("            return @event.GetType() switch");
        sourceBuilder.AppendLine("            {");
        foreach (var mapperInfo in mapperInfos)
            sourceBuilder.AppendLine($"                {{ }} type when type == typeof({mapperInfo.EventFullName}) => {mapperInfo.FieldName}.Serialize(({mapperInfo.EventFullName})@event),");
        foreach (var eventInfo in eventInfos)
        {
            if (mapperInfos.Any(x => x.EventFullName == eventInfo.FullName))
                continue;
            sourceBuilder.AppendLine($"                {{ }} type when type == typeof({eventInfo.FullName}) => {eventInfo.FieldName}.Serialize(({eventInfo.FullName})@event),");
        }
        sourceBuilder.AppendLine("                _ => throw new EventRegistryException($\"No serializer found for type {@event.GetType().Name}\")");
        sourceBuilder.AppendLine("            };");
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public IEvent Deserialize(string schema, string data)");
        sourceBuilder.AppendLine("        {");
        sourceBuilder.AppendLine("            if (!_deserializers.TryGetValue(schema, out var deserializer))");
        sourceBuilder.AppendLine("                throw new EventRegistryException($\"No deserializer found for type {schema}\");");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("            return deserializer(schema, data);");
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");

        // Add the generated source to the context
        productionContext.AddSource("EventRegistry.g.cs", sourceBuilder.ToString());
    }
}