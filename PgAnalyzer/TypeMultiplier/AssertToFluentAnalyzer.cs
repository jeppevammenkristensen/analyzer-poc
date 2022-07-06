using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Analyzers.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using PgAnalyzer.TypeMultiplier;

namespace PgAnalyzer.AssertToFluent;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeOverloadAnalyzer : SingleSharedDiagnosticAnalyzer
{
    public override DiagnosticDescriptor Descriptor { get; } = Descriptors.TypeOverload;

    protected override void DoSetup()
    {
       AddType(typeof(Type).FullName, true);
    }

    protected override void HandleStartCompilationContext(Dictionary<string, INamedTypeSymbol> types,
        CompilationStartAnalysisContext context)
    {
        context.RegisterSymbolAction(ctx => HandleSymbol(ctx, types.First().Value), SymbolKind.Method);
    }


    private void HandleSymbol(SymbolAnalysisContext ctx, INamedTypeSymbol typeSymbol)
    {
        if (ctx.Symbol is not IMethodSymbol methodSymbol) return;

        if (!methodSymbol.IsExtensionMethod) return;

        if (methodSymbol.Parameters.Length < 2) return;

        var firstParameter = methodSymbol.Parameters[0];
        if (!(methodSymbol.Parameters.Last() is { } lastParameter && lastParameter.IsParams)) return;

        if (!(lastParameter.Type is IArrayTypeSymbol arrayType &&
            SymbolEqualityComparer.IncludeNullability.Equals(arrayType.ElementType, typeSymbol)))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Descriptors.TypeOverload, typeSymbol.Locations[0]));
        }
    }
}

public class TypeOverloadCodeFix : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root?.FindNode(context.Span) is not MethodDeclarationSyntax node) return; // the span reported by analzyer

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Convert to Fluent",               
                createChangedDocument: async cancellationToken =>
                    await HandleTrue(context.Document, root, node, context.CancellationToken)), context.Diagnostics);
    }

    private async Task<Document> HandleTrue(Document document, SyntaxNode root, MethodDeclarationSyntax node, CancellationToken cancellationToken)
    {
        
        if (node.GetFirstAncestorOfType<ClassDeclarationSyntax>() is not { } original) return document;
        var modifiedOriginal = RewriteClass(original, node);
        if (modifiedOriginal is { })
        {
            return await Formatter.FormatAsync(document.WithSyntaxRoot(root.ReplaceNode(original,modifiedOriginal)), Formatter.Annotation, cancellationToken: cancellationToken);
        }

        return document;
    }

    private ClassDeclarationSyntax RewriteClass(ClassDeclarationSyntax original, MethodDeclarationSyntax node)
    {
        return null;
    }

    

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.AssertToFluent.Id);
}