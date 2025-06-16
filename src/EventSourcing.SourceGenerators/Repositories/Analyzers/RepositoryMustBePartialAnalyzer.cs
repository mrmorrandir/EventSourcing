using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EventSourcing.SourceGenerators.Repositories.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RepositoryMustBePartialAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ESSG001";
        
        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            "Repository classes must be partial",
            "Class '{0}' inherits from IRepository<T> and must be marked partial",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            if (classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (symbol == null)
                return;

            foreach (var iface in symbol.AllInterfaces)
            {
                if (iface.OriginalDefinition.ToDisplayString() == "IRepository<T>")
                {
                    var diagnostic = Diagnostic.Create(_rule, classDecl.Identifier.GetLocation(), symbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    break;
                }
            }
        }
    }
}