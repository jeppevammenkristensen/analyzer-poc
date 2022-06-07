﻿#nullable enable
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
}