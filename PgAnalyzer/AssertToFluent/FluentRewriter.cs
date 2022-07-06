using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PgAnalyzer.TypeMultiplier;

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
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.IdentifierName("FluentAssertions")))
                .NormalizeWhitespace();
            return base.VisitCompilationUnit(node);
        }

        return base.VisitCompilationUnit(node);
    }

    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node == _node)
        {
            var expression = node.ArgumentList.Arguments.FirstOrDefault()?.Expression;

            if (expression is BinaryExpressionSyntax firstExpression)
            {
                if (firstExpression.Kind() == SyntaxKind.EqualsExpression)
                {

                    if (firstExpression.Right is LiteralExpressionSyntax literal &&
                        literal.Kind() == SyntaxKind.NullLiteralExpression)
                    {
                        return base.VisitInvocationExpression(SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            firstExpression.Left,
                                            SyntaxFactory.IdentifierName("Should"))),
                                    SyntaxFactory.IdentifierName("BeNull")))
                            .NormalizeWhitespace());
                    }

                    else
                    {
                        return base.VisitInvocationExpression(SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            firstExpression.Left,
                                            SyntaxFactory.IdentifierName("Should"))),
                                    SyntaxFactory.IdentifierName("BeEquivalentTo")))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            firstExpression.Right))))
                            .NormalizeWhitespace());
                    }
                }
            }
            else if (expression is IdentifierNameSyntax identifierName)
            {
                return base.VisitInvocationExpression(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                identifierName,
                                SyntaxFactory.IdentifierName("Should"))),
                        SyntaxFactory.IdentifierName("BeTrue"))));
            }
            else if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                return base.VisitInvocationExpression(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                memberAccess,
                                SyntaxFactory.IdentifierName("Should"))),
                        SyntaxFactory.IdentifierName("BeTrue"))));
            }

        }

        return base.VisitInvocationExpression(node);
    }
}