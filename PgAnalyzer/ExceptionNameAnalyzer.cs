using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace PgAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Descriptors.ExceptionNameFormat); } }

        
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                                   GeneratedCodeAnalysisFlags.ReportDiagnostics);
             // this is where the coding starts,
            // in this case we register a handler (AnalyzeNamedType method defined below)
            // to be invoked analyzing NamedType (class, interface, delegate etc) symbols
            context.RegisterSymbolAction(action: AnalyzeNamedType, symbolKinds: SymbolKind.NamedType);
        }

        void AnalyzeNamedType(SymbolAnalysisContext ctx)
        {
            // there different kind of symbols but in this case we subscribed only for NamedType symbols
            var symbol = (INamedTypeSymbol) ctx.Symbol;
            if (symbol.TypeKind != TypeKind.Class) return;

            if (symbol.Name.EndsWith("Exception")) return;

            // as you might have noticed, in analyzer we don't work with System.Reflection model (like Types/PropertyInfos)
            // instead we work with Symbols model - a code tree structure based on text,
            // and the dope part - it exists even with code having compilation errors

            if (!IsException(
                symbol,
                ctx.Compilation.GetTypeByMetadataName(typeof(Exception).FullName))) return;

            // since we reached here -> we have an issue in code and we report it
            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    // the descriptor
                    descriptor: Descriptors.ExceptionNameFormat,
                    // current symbol location in code (file, line and column for start/end),
                    // it will become more clear further, writing tests
                    location: symbol.Locations.First(),
                    // and those are the messageFormat format args,
                    // if you remember the messageFormat was: "{0} class name should end with Exception"
                    messageArgs: symbol.Name));     
        }

        bool IsException(INamedTypeSymbol classSymbol, INamedTypeSymbol exceptionTypeSymbol)
        {
            if (classSymbol.Equals(exceptionTypeSymbol, SymbolEqualityComparer.Default)) return true;

            INamedTypeSymbol baseClass = classSymbol.BaseType;
            return baseClass != null && IsException(baseClass, exceptionTypeSymbol);
        }
    }
}
