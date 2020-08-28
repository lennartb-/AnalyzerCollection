using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace AnalyzerCollection
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerCollectionCodeFixProvider)), Shared]
    public class AnalyzerCollectionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(HasAttributeAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => AddAttributeAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Solution> AddAttributeAsync(Document document, TypeDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = classDeclaration.Identifier;

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var attributeValue = identifierToken.Text.Humanize(LetterCasing.AllCaps).Replace(' ', '_');

            var attributeArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(attributeValue)));

            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("SampleAttribute"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(attributeArgument)));

            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)).NormalizeWhitespace();
            var attributeLists = classDeclaration.AttributeLists.Add(attributeList);
            var newClassdeclaration = classDeclaration.WithAttributeLists(attributeLists);

            var formattedClassDeclaration = Formatter.Format(newClassdeclaration, MSBuildWorkspace.Create());

            return document.WithSyntaxRoot(
                root.ReplaceNode(
                    classDeclaration,
                    formattedClassDeclaration
                )).Project.Solution;
        }

    }
}
