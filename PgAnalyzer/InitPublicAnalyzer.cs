using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace PgAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InitPublicAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Descriptors.InitPublic); } }

        
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

        var method = objectCreation.Ancestors().OfType<MethodDeclarationSyntax>().First();
        var operation = context.SemanticModel.GetOperation(method);



        var declaration = objectCreation.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
        if (declaration == null) return;

        var declarationType = declaration.Type;
        var declaringType = context.SemanticModel.GetSymbolInfo(declarationType);

        if (declaringType.Symbol is INamedTypeSymbol namedType)
        {
            var propertySymbols = namedType.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod != null).ToList();
            if (!propertySymbols.Any())
                return;

            var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(declaration.Variables.First());
            if (declaredSymbol == null)
                return;
            var walker = new OpWalker(namedType, declaredSymbol.Name);
            walker.Visit(operation);
            if (propertySymbols.Select(x => x.Name).Except(walker.PropertiesSet).Any())
            {
                int i = 0;
            }
                
        }
        
    }

    public class OpWalker : OperationWalker
    {
        private readonly INamedTypeSymbol _source;
        private readonly string _name;

        public HashSet<string> PropertiesSet { get; } = new HashSet<string>();

        public OpWalker(INamedTypeSymbol source, string name)
        {
            _source = source;
            _name = name;
        }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            var memberName = _name;

            if (operation.Target is IPropertyReferenceOperation propertyReference)
            {
                if (propertyReference.Instance.Type.Equals(_source, SymbolEqualityComparer.Default) && propertyReference.Instance is ILocalReferenceOperation localRef && localRef.Local.Name == _name)
                {
                    PropertiesSet.Add(propertyReference.Property.Name);
                }
            }

            base.VisitSimpleAssignment(operation);
            
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            
            base.VisitPropertyReference(operation);
        }
    }
}