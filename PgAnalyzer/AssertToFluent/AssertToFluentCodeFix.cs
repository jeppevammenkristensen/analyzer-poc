using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace PgAnalyzer.AssertToFluent;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertToFluentCodeFix)), Shared]
public class AssertToFluentCodeFix : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root?.FindNode(context.Span) is not InvocationExpressionSyntax node) return; // the span reported by analzyer

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Convert to Fluent",               
                createChangedDocument: async cancellationToken =>
                    await HandleTrue(context.Document, root, node, context.CancellationToken)), context.Diagnostics);
    }

    private async Task<Document> HandleTrue(Document context, SyntaxNode root, InvocationExpressionSyntax node, CancellationToken cancellationToken)
    {
        var fluentRewriter = new FluentRewriter(node);
        return await Formatter.FormatAsync(context.WithSyntaxRoot(fluentRewriter.Visit(root)), Formatter.Annotation, cancellationToken: cancellationToken);
    }

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.AssertToFluent.Id);
}