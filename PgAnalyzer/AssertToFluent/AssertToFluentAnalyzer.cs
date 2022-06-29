using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading;
using Analyzers.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PgAnalyzer.AssertToFluent;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AssertToFluentAnalyzer : SingleSharedDiagnosticAnalyzer
{
    public const string XunitAssert = "Xunit.Assert";
    public const string NUnitAssert = "NUnit.Framework.Assert";

    public override DiagnosticDescriptor Descriptor { get; } = Descriptors.AssertToFluent;

    protected override void DoSetup()
    {
        AddType(XunitAssert, false);
        AddType("FluentAssertions.AssertionExtensions", true);
        AddType(NUnitAssert,false);
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

        INamedTypeSymbol? xunit = null;

        if (types.ContainsKey(XunitAssert))
        {
            xunit = types[XunitAssert];
        }

        INamedTypeSymbol? nunit = null;
        if (types.ContainsKey(NUnitAssert))
        {
            nunit = types[NUnitAssert];
        }
        

        if (invocationOperation.TargetMethod.Name == "True" && invocationOperation.TargetMethod.ReceiverType is INamedTypeSymbol type &&
            (type.Equals(xunit, SymbolEqualityComparer.IncludeNullability) || type.Equals(nunit, SymbolEqualityComparer.IncludeNullability)))
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