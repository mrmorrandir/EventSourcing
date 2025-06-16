using System.Collections.Immutable;
        using System.Composition;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.CodeAnalysis;
        using Microsoft.CodeAnalysis.CodeActions;
        using Microsoft.CodeAnalysis.CodeFixes;
        using Microsoft.CodeAnalysis.CSharp;
        using Microsoft.CodeAnalysis.CSharp.Syntax;
        
        namespace EventSourcing.Analyzers
        {
            [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RepositoryMustBePartialCodeFixProvider)), Shared]
            public class RepositoryMustBePartialCodeFixProvider : CodeFixProvider
            {
                public sealed override ImmutableArray<string> FixableDiagnosticIds
                    => ImmutableArray.Create(RepositoryMustBePartialAnalyzer.DiagnosticId);
        
                public sealed override FixAllProvider GetFixAllProvider()
                    => WellKnownFixAllProviders.BatchFixer;
        
                public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
                {
                    var diagnostic = context.Diagnostics[0];
                    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                    var node = root.FindNode(diagnostic.Location.SourceSpan) as ClassDeclarationSyntax;
                    if (node == null)
                        return;
        
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Mark class as partial",
                            c => MakePartialAsync(context.Document, node, c),
                            nameof(RepositoryMustBePartialCodeFixProvider)),
                        diagnostic);
                }
        
                private async Task<Document> MakePartialAsync(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
                {
                    var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space);
        
                    var newModifiers = classDecl.Modifiers.Add(partialToken);
                    var newClassDecl = classDecl.WithModifiers(newModifiers);
        
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var newRoot = root.ReplaceNode(classDecl, newClassDecl);
        
                    return document.WithSyntaxRoot(newRoot);
                }
            }
        }