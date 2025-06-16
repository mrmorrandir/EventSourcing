using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventSourcing.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MustOverrideProjectAsyncAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ESSG003";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Class must override ProjectAsync",
            "Class '{0}' inherits from AbstractProjection but does not override ProjectAsync",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
        }

        private static void AnalyzeClass(SymbolAnalysisContext context)
        {
            var classSymbol = (INamedTypeSymbol)context.Symbol;
            if (classSymbol.TypeKind != TypeKind.Class)
                return;

            // Check if inherits from AbstractProjection
            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == "AbstractProjection")
                    break;
                baseType = baseType.BaseType;
            }
            if (baseType == null || baseType.Name != "AbstractProjection")
                return;

            // Check for override of ProjectAsync
            var overridesProjectAsync = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Any(m => m.Name == "ProjectAsync" && m.IsOverride);

            if (!overridesProjectAsync)
            {
                var diagnostic = Diagnostic.Create(Rule, classSymbol.Locations[0], classSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}