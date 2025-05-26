using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EventSourcing.SourceGenerators;

//[Generator]
public partial class EventMapperGenerator : IIncrementalGenerator
{
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*-v[0-9]+$")]
    private static partial Regex TypeRegex();

    [GeneratedRegex(@"-v[0-9]+$")]
    private static partial Regex VersionSuffixRegex();
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all Types that derive from AbstractEventMapper<T>
        var eventClassDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, ct) => IsEventCandidate(s, ct),
                static (ctx, ct) => GetEventTypeDeclaration(ctx, ct))
            .Where(static x => x is not null)
            .Select((x, ct) => x!);

        var compilation = context.CompilationProvider.Combine(eventClassDeclarations.Collect());

        context.RegisterSourceOutput(compilation, CreateSource);
    }
    
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
    
    private static string ToKebabCase(string type, bool withVersion = true)
    {
        var kebabCaseName = string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        // Check if the kebab case name already has a version number with a regex
        if (!VersionSuffixRegex().IsMatch(kebabCaseName) && withVersion)
            kebabCaseName += "-v1"; // default versioning
        return kebabCaseName;
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

    private static void CreateSource(SourceProductionContext productionContext, (Compilation compilation, ImmutableArray<TypeDeclarationSyntax> eventClassDeclarationSyntaxes) combinedData)
    {
        var (compilation, events) = combinedData;

        // Extract event class names
        var eventInfos = new List<EventInfoX>();
        foreach (var eventClassDeclarationSyntax in events)
        {
            var @namespace = GetNamespace(eventClassDeclarationSyntax);
            var name = $"{eventClassDeclarationSyntax.Identifier.Text}";
            var fullName = $"{@namespace}.{name}";
            var mapperFieldName = $"_{char.ToLower(fullName[0]) + fullName[1..].Replace(".","")}";
            if (!mapperFieldName.EndsWith("Mapper"))
                mapperFieldName += "Mapper";
            var kebabCaseName = ToKebabCase(name);
            eventInfos.Add(new EventInfoX(@namespace, fullName, name, mapperFieldName, kebabCaseName));
        }

        
        foreach (var eventInfo in eventInfos)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using EventSourcing.Mappers;");
            sb.AppendLine();
            sb.AppendLine($"using {eventInfo.Namespace};");
            sb.AppendLine();
            sb.AppendLine($"namespace {eventInfo.Namespace};");
            sb.AppendLine();
            sb.AppendLine($"public partial class {eventInfo.Name}Mapper : AbstractEventMapper<{eventInfo.Name}>");
            sb.AppendLine( "{");
            sb.AppendLine($"    public {eventInfo.Name}Mapper()");
            sb.AppendLine( "    {");
            sb.AppendLine($"        WillSerialize(\"{eventInfo.KebabCaseName}\");");
            sb.AppendLine($"        CanDeserialize(\"{eventInfo.KebabCaseName}\");");
            sb.AppendLine($"        Configure();");
            sb.AppendLine( "    }");
            sb.AppendLine();
            sb.AppendLine($"    partial void Configure();");
            sb.AppendLine( "}");
            productionContext.AddSource($"{eventInfo.Namespace}.{eventInfo.Name}Mapper.g.cs", sb.ToString());
            
        }
    }
}