using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventSourcing.SourceGenerators.Common;

public static class NamespaceHelper
{
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
}