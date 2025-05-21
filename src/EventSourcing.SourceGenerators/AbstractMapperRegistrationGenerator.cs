using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventSourcing.SourceGenerators;

[Generator]
public class AbstractMapperRegistrationGenerator : IIncrementalGenerator
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
        return (node is TypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.BaseList?.Types.Any(baseTypeSyntax => baseTypeSyntax.ToString().Contains("IEvent")) == true);
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
    
    private static void CreateSource(SourceProductionContext productionContext, ((Compilation compilation, ImmutableArray<ClassDeclarationSyntax> abstractMappersClassDeclarationSyntaxes) abstractMappersTuple, ImmutableArray<TypeDeclarationSyntax> eventClassDeclarationSyntaxes) combinedData)
    {
        var (compilation, abstractMappersClassDeclarationSyntaxes) = combinedData.abstractMappersTuple;
        var events = combinedData.eventClassDeclarationSyntaxes;

        // Extract full class names (namespace + class name)
        var mapperClassFullNames = abstractMappersClassDeclarationSyntaxes
            .Select(classDeclaration =>
            {
                var namespaceName = GetNamespace(classDeclaration);
                return new
                {
                    Fullname = $"{namespaceName}.{classDeclaration.Identifier.Text}",
                    Name = $"{classDeclaration.Identifier.Text}",
                };
            })
            .ToList();
        
        // Extract event class names
        var eventClassFullNames = events
            .Select(classDeclaration =>
            {
                var namespaceName = GetNamespace(classDeclaration);
                return new
                {
                    Fullname = $"{namespaceName}.{classDeclaration.Identifier.Text}",
                    Name = $"{classDeclaration.Identifier.Text}",
                };
            })
            .ToList();

        // Generate the EventRegistry2 class
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Collections.Generic;");
        sourceBuilder.AppendLine("using EventSourcing.Mappers;");
        sourceBuilder.AppendLine("using FluentResults;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace EventSourcing.Generated");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("    public class EventRegistry");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        private readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();");
        sourceBuilder.AppendLine("        private readonly Dictionary<Type, Func<IEvent, ISerializedEvent>> _serializers = new();");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public EventRegistry()");
        sourceBuilder.AppendLine("        {");
        sourceBuilder.AppendLine("            InitializeAbstractEventMappers();");
        sourceBuilder.AppendLine("            InitializeDefaultEventMappers();");
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        private void InitializeAbstractEventMappers()");
        sourceBuilder.AppendLine("        {");
        
        // Add each mapper to the dictionaries
        var mapperNumber = 1;
        foreach (var mapperClass in mapperClassFullNames)
        {
            sourceBuilder.AppendLine($"            var mapper{mapperNumber} = new {mapperClass.Fullname}();");
            sourceBuilder.AppendLine($"            foreach (var typeSchema in mapper{mapperNumber}.Types)");
            sourceBuilder.AppendLine($"                _deserializers[typeSchema] = (type, data) => mapper{mapperNumber}.Deserialize(type, data);");
            sourceBuilder.AppendLine($"            _serializers[mapper{mapperNumber}.EventType] = (@event) => mapper{mapperNumber}.Serialize((dynamic)@event);");
            sourceBuilder.AppendLine($"");
            mapperNumber++;
        }
        
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        private void InitializeDefaultEventMappers()");
        sourceBuilder.AppendLine("        {");
        
        // Add each mapper to the dictionaries
        mapperNumber = 1;
        foreach (var eventClass in eventClassFullNames)
        {
            sourceBuilder.AppendLine($"            if (!_serializers.ContainsKey(typeof({eventClass.Fullname})))");
            sourceBuilder.AppendLine($"            {{");
            sourceBuilder.AppendLine($"                var mapper{mapperNumber} = new DefaultEventMapper<{eventClass.Fullname}>();");
            sourceBuilder.AppendLine($"                foreach (var typeSchema in mapper{mapperNumber}.Types)");
            sourceBuilder.AppendLine($"                    _deserializers[typeSchema] = (type, data) => mapper{mapperNumber}.Deserialize(type, data);");
            sourceBuilder.AppendLine($"                _serializers[mapper{mapperNumber}.EventType] = (@event) => mapper{mapperNumber}.Serialize((dynamic)@event);");
            sourceBuilder.AppendLine($"            }}");
            sourceBuilder.AppendLine($"");
            mapperNumber++;
        }
        
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public ISerializedEvent Serialize(IEvent @event)");
        sourceBuilder.AppendLine("        {");
        sourceBuilder.AppendLine("            if (!_serializers.TryGetValue(@event.GetType(), out var serializer))");
        sourceBuilder.AppendLine("                throw new InvalidOperationException($\"No serializer found for type {@event.GetType().Name}\");");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("            return serializer(@event);");
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public IEvent Deserialize(string type, string data)");
        sourceBuilder.AppendLine("        {");
        sourceBuilder.AppendLine("            if (!_deserializers.TryGetValue(type, out var deserializer))");
        sourceBuilder.AppendLine("                throw new InvalidOperationException($\"No deserializer found for type {type}\");");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("            return deserializer(type, data);");
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");

        // Add the generated source to the context
        productionContext.AddSource("EventRegistry.g.cs", sourceBuilder.ToString());
    }

}