using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Analyzers.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PgAnalyzer.AssertToFluent;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AssertToFluentAnalyzer : SingleSharedDiagnosticAnalyzer
{
    public const string XunitAssert = "Xunit.Assert";

    public override DiagnosticDescriptor Descriptor { get; } = Descriptors.AssertToFluent;

    protected override void DoSetup()
    {
        AddType(XunitAssert);
        AddType("FluentAssertions.AssertionExtensions");
    }

    protected override void HandleStartCompilationContext(Dictionary<string, INamedTypeSymbol> types,
        CompilationStartAnalysisContext context)
    {
        context.RegisterOperationAction(ctx => HandleOperation(ctx, ctx.CancellationToken, types),
            OperationKind.Invocation);
    }

    private void HandleOperation(OperationAnalysisContext ctx, CancellationToken objCancellationToken,
        Dictionary<string, INamedTypeSymbol> types)
    {
        if (ctx.Operation is not IInvocationOperation invocationOperation) return;

        var xunit = types[XunitAssert];

        if (invocationOperation.TargetMethod.Name == "True" && invocationOperation.TargetMethod.ReceiverType is INamedTypeSymbol type &&
            type.Equals(xunit, SymbolEqualityComparer.IncludeNullability))
        {
            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    // the descriptor
                    descriptor: Descriptor,
                    // current symbol location in code (file, line and column for start/end),
                    // it will become more clear further, writing tests
                    location: ctx.Operation.Syntax.GetLocation()
                    // and those are the messageFormat format args,
                    // if you remember the messageFormat was: "{0} class name should end with Exception"
                ));
        }
    }
}

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
                // the solution is in immutable hierarchical model of everything (includes projects, documents and every syntax node)
                // the delegate should return a new (modified) model based on the initial one,
                // if you used react/redux - this should be very familair
                // in most of cases you don't need to write a code for it, therefore there are utilities like Renamer (used below)
                createChangedDocument: async cancellationToken =>
                    await HandleTrue(context.Document, root, node, context.CancellationToken)), context.Diagnostics);
    }

    private async Task<Document> HandleTrue(Document context, SyntaxNode root, InvocationExpressionSyntax node, CancellationToken cancellationToken)
    {
        var fluentRewriter = new FluentRewriter(node);
        return context.WithSyntaxRoot(fluentRewriter.Visit(root));
    }

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.AssertToFluent.Id);
}

public class FluentRewriter : CSharpSyntaxRewriter
{
    private readonly InvocationExpressionSyntax _node;

    public FluentRewriter(InvocationExpressionSyntax node)
    {
        _node = node;
    }

    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        if (node.Usings.All(x => x.Name.ToString() != "FluentAssertions"))
        {
            node = node
                .AddUsings(
                    UsingDirective(
                            IdentifierName("FluentAssertions")))
                .NormalizeWhitespace();
            return base.VisitCompilationUnit(node);
        } 

        return base.VisitCompilationUnit(node);
    }

    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node == _node && node.ArgumentList.Arguments.FirstOrDefault()?.Expression is BinaryExpressionSyntax firstExpression)
        {
            if (firstExpression.Kind() == SyntaxKind.EqualsExpression)
            {
                

                return base.VisitInvocationExpression(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    firstExpression.Left,
                                    IdentifierName("Should"))),
                            IdentifierName("BeEquivalentTo")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList<ArgumentSyntax>(
                                Argument(
                                    firstExpression.Right))))
                    .NormalizeWhitespace());
            }
        }

        return base.VisitInvocationExpression(node);
    }
}