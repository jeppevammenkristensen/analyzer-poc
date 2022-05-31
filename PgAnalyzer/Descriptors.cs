﻿using System;
using Microsoft.CodeAnalysis;

namespace PgAnalyzer
{
    internal static class Descriptors
    {
        internal static readonly DiagnosticDescriptor ExceptionNameFormat = new DiagnosticDescriptor(
            "JJk0001", "Exception class name should end with exception", "{0} should end with exception",
            category: "Naming", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    }
}