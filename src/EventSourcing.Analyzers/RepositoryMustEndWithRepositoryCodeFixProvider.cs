using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace EventSourcing.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RepositoryMustEndWithRepositoryCodeFixProvider)), Shared]
    public class RepositoryMustEndWithRepositoryCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(RepositoryMustEndWithRepositoryAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan) as ClassDeclarationSyntax;
            if (node == null)
                return;

            var newName = node.Identifier.Text;
            if (newName.EndsWith("Rep"))
                newName = $"{newName}o";
            if (newName.EndsWith("Repo"))
                newName = $"{newName}sitory";
            if (!newName.EndsWith("Repository"))
                newName = $"{newName}Repository";

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Rename to '{newName}'",
                    c => RenameClassAsync(context.Document, node, newName, c),
                    nameof(RepositoryMustEndWithRepositoryCodeFixProvider)),
                diagnostic);
        }

        private async Task<Solution> RenameClassAsync(Document document, ClassDeclarationSyntax classDecl, string newName, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document.Project.Solution;
            
            var symbol = semanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
            if (symbol == null)
                return document.Project.Solution;
            
            var solution = document.Project.Solution;
            return await Renamer.RenameSymbolAsync(solution, symbol, new SymbolRenameOptions(RenameFile: true), newName, cancellationToken).ConfigureAwait(false);
        }
    }
}