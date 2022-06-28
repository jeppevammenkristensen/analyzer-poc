using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading;
using Analyzers.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PgAnalyzer.AssertToFluent;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeOverloadAnalyzer : SingleSharedDiagnosticAnalyzer
{
   

    public override DiagnosticDescriptor Descriptor { get; } = Descriptors.TypeOverload;

    protected override void DoSetup()
    {
       AddType(typeof(Type).FullName);
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