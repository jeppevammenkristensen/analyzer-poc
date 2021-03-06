using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PgAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CheckKeyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Descriptors.CheckKey); }
    }


    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var typeSymbol =
                compilationContext.Compilation.GetTypeByMetadataName("System.Collections.IDictionary");

            if (typeSymbol != null)
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
            }
        });
        // this is where the coding starts,
        // in this case we register a handler (AnalyzeNamedType method defined below)
        // to be invoked analyzing NamedType (class, interface, delegate etc) symbols
       

    }

    private void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ElementAccessExpressionSyntax elementAccess || elementAccess.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        if (elementAccess.Parent is AssignmentExpressionSyntax aes && aes.Left == elementAccess)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(elementAccess.Expression);
        if (typeInfo.Type == null)
            return;
        var dictionary =
            context.Compilation.GetTypeByMetadataName("System.Collections.IDictionary");
        var genericDictionary =
            context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");

        if (!typeInfo.Type.Implements(dictionary!))
        {
            if (!typeInfo.Type.ImplementsOrIs(s =>
                {
                    return s is INamedTypeSymbol ns &&
                           SymbolEqualityComparer.Default.Equals(ns.ConstructedFrom, genericDictionary);
                }))
            {
                return;
            }
        } 


        if (context.Node.GetFirstAncestorOfType<MethodDeclarationSyntax>() is not { } block) return;

        var walker = new DictionaryWalker(context.SemanticModel.GetOperation(context.Node));
        walker.Visit(context.SemanticModel.GetOperation((block.Body as SyntaxNode ?? block.ExpressionBody) ?? 
                                                        
            null));

        if (walker.ChecksKey) return;



        context.ReportDiagnostic(
            Diagnostic.Create(
                // the descriptor
                descriptor: Descriptors.CheckKey,
                // current symbol location in code (file, line and column for start/end),
                // it will become more clear further, writing tests
                location: context.Node.GetLocation()
                // and those are the messageFormat format args,
                // if you remember the messageFormat was: "{0} class name should end with Exception"
                ));
    }

    public class  DictionaryWalker : OperationWalker
    {
        private readonly IOperation _operation;

        public DictionaryWalker(IOperation operation)
        {
            _operation = operation;
        }

        public bool ChecksKey { get; private set; } = false;

        public override void Visit(IOperation operation)
        {
            if (operation == _operation) return;
            base.Visit(operation);
        }

        public override void VisitExpressionStatement(IExpressionStatementOperation operation)
        {
            base.VisitExpressionStatement(operation);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name == "ContainsKey")
            {
                ChecksKey = true;
                return;
            }
            base.VisitInvocation(operation);
        }
    }
}

