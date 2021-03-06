#nullable enable
using System;
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