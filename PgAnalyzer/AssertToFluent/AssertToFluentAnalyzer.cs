using System.Collections.Generic;
using System.Linq;
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

    public override DiagnosticDescriptor Descriptor { get; } = Descriptors.AssertToFluent;

    protected override void DoSetup()
    {
        AddType(XunitAssert);
        AddType("Fluent.AssertionExtensions");
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

        if (invocationOperation.TargetMethod.Name == "True" && invocationOperation.Type is INamedTypeSymbol type &&
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