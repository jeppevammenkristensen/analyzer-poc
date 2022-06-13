using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace PgAnalyzer;

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
            if (propertyReference.Instance.Type.Equals(_source, SymbolEqualityComparer.Default) &&
                propertyReference.Instance is ILocalReferenceOperation localRef && localRef.Local.Name == _name)
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