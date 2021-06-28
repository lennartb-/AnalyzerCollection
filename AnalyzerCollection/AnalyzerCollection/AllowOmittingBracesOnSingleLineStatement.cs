using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerCollection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class AllowOmittingBracesOnSingleLineStatement : DiagnosticAnalyzer
    {
        /// <summary>
        /// The ID for diagnostics produced by the <see cref="AllowOmittingBracesOnSingleLineStatement"/> analyzer.
        /// </summary>
        public const string DiagnosticId = "SA1503";

        private const string HelpLink = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1503.md";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AC0002AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AC0002MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AC0002Description), Resources.ResourceManager, typeof(Resources));
        internal static readonly DiagnosticDescriptor Descriptor = new(DiagnosticId, Title, MessageFormat, "LayoutRules", DiagnosticSeverity.Warning, true, Description, HelpLink);
        private static readonly Action<SyntaxNodeAnalysisContext> IfStatementAction = HandleIfStatement;
        private static readonly Action<SyntaxNodeAnalysisContext> UsingStatementAction = HandleUsingStatement;

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(IfStatementAction, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(ctx => CheckChildStatement(ctx, ((DoStatementSyntax) ctx.Node).Statement), SyntaxKind.DoStatement);
            context.RegisterSyntaxNodeAction(ctx => CheckChildStatement(ctx, ((WhileStatementSyntax) ctx.Node).Statement), SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(ctx => CheckChildStatement(ctx, ((ForStatementSyntax) ctx.Node).Statement), SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(ctx => CheckChildStatement(ctx, ((ForEachStatementSyntax) ctx.Node).Statement), SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(ctx => CheckChildStatement(ctx, ((FixedStatementSyntax) ctx.Node).Statement), SyntaxKind.FixedStatement);
            context.RegisterSyntaxNodeAction(UsingStatementAction, SyntaxKind.UsingStatement);
            context.RegisterSyntaxNodeAction(ctx => CheckChildStatement(ctx, ((LockStatementSyntax) ctx.Node).Statement), SyntaxKind.LockStatement);
        }

        private static void HandleIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax) context.Node;
            if (ifStatement.Parent.IsKind(SyntaxKind.ElseClause))
            {
                // this will be analyzed as a clause of the outer if statement
                return;
            }

            List<StatementSyntax> clauses = new List<StatementSyntax>();
            for (IfStatementSyntax current = ifStatement; current != null; current = current.Else?.Statement as IfStatementSyntax)
            {
                clauses.Add(current.Statement);
                if (current.Else != null && !(current.Else.Statement is IfStatementSyntax))
                {
                    clauses.Add(current.Else.Statement);
                }
            }

            // inconsistencies will be reported as SA1520, as long as it's not suppressed
            if (clauses.OfType<BlockSyntax>().Any())
            {
                return;
            }

            foreach (StatementSyntax clause in clauses)
            {
                CheckChildStatement(context, clause);
            }
        }

        private static void HandleUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax) context.Node;
            if (usingStatement.Statement.IsKind(SyntaxKind.UsingStatement))
            {
                return;
            }

            CheckChildStatement(context, usingStatement.Statement);
        }

        private static void CheckChildStatement(SyntaxNodeAnalysisContext context, StatementSyntax childStatement)
        {
            if (childStatement is BlockSyntax)
            {
                return;
            }

            // diagnostics for multi-line statements is handled by SA1519, as long as it's not suppressed
            FileLinePositionSpan lineSpan = childStatement.SyntaxTree.GetLineSpan(childStatement.Span);
            if (lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, childStatement.GetLocation()));
        }
    }
}