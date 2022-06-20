using System;
using Microsoft.CodeAnalysis;

namespace PgAnalyzer
{
    internal static class Descriptors
    {
        internal static readonly DiagnosticDescriptor ExceptionNameFormat = new DiagnosticDescriptor(
            "JJk0001", "Exception class name should end with exception", "{0} should end with exception",
            category: "Naming", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InitPublic = new DiagnosticDescriptor("JJK0002",
            "Not all properties transferred", "Not all properties has been set. Missing are {0}.", "Init", DiagnosticSeverity.Warning,
            true);        

        internal static readonly DiagnosticDescriptor CheckKey = new DiagnosticDescriptor("JJK0003",
            "Key not checked before call", "Did not check if the key was present.", "Dictionary", DiagnosticSeverity.Warning,
            true);

        internal static readonly DiagnosticDescriptor AssertToFluent = new DiagnosticDescriptor("JJK0004",
            "Use fluent instead", "Use fluent instead", "Dictionary", DiagnosticSeverity.Warning,
            true);

    }
}
