using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PgAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitPublicCodeFix)), Shared]
public class InitPublicCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(Descriptors.InitPublic.Id); }
    }

    //public sealed override FixAllProvider GetFixAllProvider()
    //{
    //    return WellKnownFixAllProviders.BatchFixer;
    //}

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        SyntaxNode node = root?.FindNode(context.Span); // the span reported by analzyer

        if (node.GetFirstAncestorOfType<ObjectCreationExpressionSyntax>() is not { } objectCreation) return;

        var semanticModel = await context.Document.GetSemanticModelAsync();
        var operation = new GetPropertiesNotSet(semanticModel, objectCreation);
        operation.Run();
        if (operation.ExtraProperties.IsEmpty)
            return;

        if (objectCreation.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault() is not
            { } variableDeclaration) return;
        if (variableDeclaration.Variables.FirstOrDefault() is not { } variable) return;

        if (variable.Ancestors().OfType<BlockSyntax>().FirstOrDefault() is not { } block) return;


        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Set properties to ",
                // the solution is in immutable hierarchical model of everything (includes projects, documents and every syntax node)
                // the delegate should return a new (modified) model based on the initial one,
                // if you used react/redux - this should be very familair
                // in most of cases you don't need to write a code for it, therefore there are utilities like Renamer (used below)
                createChangedDocument: async cancellationToken =>
                    await AddProperties(cancellationToken, context.Document, semanticModel, objectCreation,
                        operation.VariableName, operation.NamedType, operation.ExtraProperties)),
            context.Diagnostics);

        await Task.CompletedTask;
    }

    private async Task<Document> AddProperties(CancellationToken token, Document document, SemanticModel model,
        ObjectCreationExpressionSyntax objectCreation, string variableName, INamedTypeSymbol symbol,
        ICollection<string> properties)
    {
        var propertySymbols = symbol.GetMembers()
            .OfType<IPropertySymbol>().Where(x => !x.IsReadOnly && properties.Contains(x.Name)).ToList();

        if (objectCreation.GetFirstAncestorOfType<LocalDeclarationStatementSyntax>() is { } localDecl
            && localDecl.GetFirstAncestorOfType<BlockSyntax>() is { } block)
        {
            //block.InsertNodesAfter(localDecl, p)
            var nodes = new List<SyntaxNode>();

            foreach (IPropertySymbol propertySymbol in propertySymbols)
            {
                nodes.Add(SyntaxFactory.ExpressionStatement
                    (
                        SyntaxFactory.AssignmentExpression
                        (
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression
                            (
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(variableName),
                                SyntaxFactory.IdentifierName(propertySymbol.Name)
                            ),
                            SyntaxFactory.LiteralExpression
                            (
                                SyntaxKind.NullLiteralExpression
                            )
                        )
                    )
                );
            }

            var newBlock = block.InsertNodesAfter(localDecl, nodes).NormalizeWhitespace();
            if (await document.GetSyntaxRootAsync(token) is { } syntaxRootAsync)
            {
                return document.WithSyntaxRoot(syntaxRootAsync.ReplaceNode(block, newBlock));
            }
        }

        return document;
    }
}