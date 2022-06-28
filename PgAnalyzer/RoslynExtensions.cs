#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PgAnalyzer;

public static class RoslynExtensions
{
    public static T? GetFirstAncestorOfType<T>(this SyntaxNode source) where T : SyntaxNode
    {
        return source.Ancestors().OfType<T>().FirstOrDefault();
    }

    public static bool IsAnyOfType<TType1,TType2>(this IEnumerable<SyntaxNode> source) where TType1 : SyntaxNode where TType2 : SyntaxNode
    {
        return source.IsAnyOfType(typeof(TType1), typeof(TType2));

    }

    public static bool IsAnyOfType(this IEnumerable<SyntaxNode> source, params Type[] types)
    {
        return types.Any(type => type == source.GetType());

    }

    public static bool Implements(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(type,i));
    }

    public static bool ImplementsOrIs(this ITypeSymbol symbol,
        Func<ITypeSymbol, bool> comparer)
    {
        if (comparer(symbol))
            return true;

        return symbol.AllInterfaces.Any(i => comparer(i));
    }
}