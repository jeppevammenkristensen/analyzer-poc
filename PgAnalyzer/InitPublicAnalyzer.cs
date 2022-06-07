using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PgAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InitPublicAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Descriptors.InitPublic); }
    }


    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);
        // this is where the coding starts,
        // in this case we register a handler (AnalyzeNamedType method defined below)
        // to be invoked analyzing NamedType (class, interface, delegate etc) symbols
        context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ObjectCreationExpression);
    }

    private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax objectCreation)
        {
            return;
        }

        var getPropertiesNotSet = new GetPropertiesNotSet(context.SemanticModel, objectCreation);
        getPropertiesNotSet.Run();

        if (getPropertiesNotSet.ExtraProperties.IsEmpty)
            return;


        context.ReportDiagnostic(
            Diagnostic.Create(
                // the descriptor
                descriptor: Descriptors.InitPublic,
                // current symbol location in code (file, line and column for start/end),
                // it will become more clear further, writing tests
                location: objectCreation.Type.GetLocation(),
                // and those are the messageFormat format args,
                // if you remember the messageFormat was: "{0} class name should end with Exception"
                messageArgs: string.Join(",", getPropertiesNotSet.ExtraProperties.Select(x => x))));
    }
}

