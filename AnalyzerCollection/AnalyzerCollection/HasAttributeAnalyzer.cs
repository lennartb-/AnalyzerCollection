using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerCollection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HasAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AnalyzerCollection";

        private const string InterfaceName = "AnalyzerCollection.HasAttribute.ISampleInterface";
        private const string AttributeName = "SampleAttribute";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AC0001AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AC0001MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AC0001Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol) context.Symbol;
            var attributes = namedTypeSymbol.GetAttributes();

            if (namedTypeSymbol.AllInterfaces.All(i => i.ToDisplayString() != InterfaceName))
            {
                return;
            }

            if (attributes.All(a => a.AttributeClass?.Name != AttributeName))
            {
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], AttributeName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
