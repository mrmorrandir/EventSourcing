using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EventSourcing.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RepositoryMustEndWithRepositoryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ESSG002";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Repository classes must end with 'Repository'",
            "Class '{0}' inherits from IRepository<T> and must end with 'Repository'",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDecl = context.Node as ClassDeclarationSyntax;
            if (classDecl == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol == null)
                return;

            var allInterfaces = symbol.AllInterfaces;
            if (allInterfaces == null)
                return;

            foreach (var iface in allInterfaces)
            {
                var originalDef = iface.OriginalDefinition;
                if (originalDef.Name == "IRepository" && !symbol.Name.EndsWith("Repository"))
                {
                    var diagnostic = Diagnostic.Create(_rule, classDecl.Identifier.GetLocation(), symbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    break;
                }
            }
        }
    }
}