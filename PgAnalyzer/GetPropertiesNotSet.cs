using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace PgAnalyzer;

public class GetPropertiesNotSet
{
    private readonly SemanticModel _semanticModel;
    private readonly BaseObjectCreationExpressionSyntax _objectCreation;

    public GetPropertiesNotSet(SemanticModel semanticModel, BaseObjectCreationExpressionSyntax objectCreation)
    {
        _semanticModel = semanticModel;
        _objectCreation = objectCreation;
    }

    public void Run()
    {
        if (_objectCreation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not
            { } method) return;
        var operation = _semanticModel.GetOperation(method);

        // Contains the property names for properties initalized somehow
        var initializedProperties = new HashSet<string>();

        var constructor = _semanticModel.GetOperation(_objectCreation) as IObjectCreationOperation;
        var oper = _semanticModel.GetOperation(constructor.Constructor.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax());

        

        if (_objectCreation.Initializer is {} initalizer)
        {
            foreach (var property in initalizer.Expressions.OfType<AssignmentExpressionSyntax>().Select(x => x.Left is IdentifierNameSyntax identi ? identi.Identifier.Text : null).Where(x => x != null))
            {
                initializedProperties.Add(property);
            }
        }
        // Runs through the properties initalized from the constructor
                var declaration = _objectCreation.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
        if (declaration == null) return;

        var declarationType = declaration.Type;
        var declaringType = _semanticModel.GetSymbolInfo(declarationType);

        if (declaringType.Symbol is INamedTypeSymbol namedType)
        {
            var propertySymbols = namedType.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod != null && x.DeclaredAccessibility == Accessibility.Public)
                .ToList();
            if (!propertySymbols.Any())
                return;

            var declaredSymbol = _semanticModel.GetDeclaredSymbol(declaration.Variables.First());
            if (declaredSymbol == null)
                return;
            NamedType = namedType;
            VariableName = declaredSymbol.Name;

            var walker = new OpWalker(namedType, declaredSymbol.Name);
            walker.Visit(operation);
             walker.Visit(oper);
            


            if (_objectCreation.Initializer is { } &&
                _semanticModel.GetOperation(_objectCreation.Initializer) is IObjectOrCollectionInitializerOperation
                    initializerOperation)
            {
                walker.Visit(initializerOperation);


                //foreach (var simpleAssignmentOperation in initializerOperation.Initializers
                //             .OfType<ISimpleAssignmentOperation>())
                //{
                //    if (simpleAssignmentOperation.Target is IPropertyReferenceOperation propertyReference)
                //    {
                //        initializedProperties.Add(propertyReference.Property.Name);
                //    }
                //}
            }




            
                       

            ExtraProperties = propertySymbols.Select(x => x.Name)
                .Except(walker.PropertiesSet.Union(initializedProperties)).ToImmutableList();
        }
    }

    public string? VariableName { get; private set; }

    public INamedTypeSymbol? NamedType { get; private set; }

    public ImmutableList<string> ExtraProperties { get; private set; } = ImmutableList<string>.Empty;


}