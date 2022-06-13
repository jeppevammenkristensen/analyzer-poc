using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PgAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CheckKeyCodeFix)), Shared]
public class CheckKeyCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(Descriptors.CheckKey.Id); }
    }

    //public sealed override FixAllProvider GetFixAllProvider()
    //{
    //    return WellKnownFixAllProviders.BatchFixer;
    //}

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root?.FindNode(context.Span) is not ElementAccessExpressionSyntax node) return; // the span reported by analzyer

        if (node.GetFirstAncestorOfType<StatementSyntax>() is not { } statement) return; 

        var semanticModel = await context.Document.GetSemanticModelAsync();


        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Wrap in if check",
                // the solution is in immutable hierarchical model of everything (includes projects, documents and every syntax node)
                // the delegate should return a new (modified) model based on the initial one,
                // if you used react/redux - this should be very familair
                // in most of cases you don't need to write a code for it, therefore there are utilities like Renamer (used below)
                createChangedDocument: async cancellationToken =>
                    await WrapInCheck(context.Document, root, node, statement, context.CancellationToken)), context.Diagnostics);

        await Task.CompletedTask;
    }

    private async Task<Document> WrapInCheck(Document document, SyntaxNode root, ElementAccessExpressionSyntax elementAccess, StatementSyntax statement, CancellationToken cancellationToken)
    {
        var ifExpression = SyntaxFactory.IfStatement
        (
            SyntaxFactory.InvocationExpression
                (
                    SyntaxFactory.MemberAccessExpression
                    (
                        SyntaxKind.SimpleMemberAccessExpression,
                        elementAccess.Expression,
                        SyntaxFactory.IdentifierName("ContainsKey")
                    )
                )
                .WithArgumentList
                (
                    SyntaxFactory.ArgumentList
                    (
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>
                        (

                            elementAccess.ArgumentList.Arguments.First()

                        )
                    )
                ),
            SyntaxFactory.Block().AddStatements(statement)
        );

        return document.WithSyntaxRoot(root.ReplaceNode(statement, ifExpression));
    }

}