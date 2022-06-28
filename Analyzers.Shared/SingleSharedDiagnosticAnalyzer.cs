using System;using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers.Shared
{
    public abstract class SingleSharedDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private ImmutableArray<DiagnosticDescriptor> _descriptors;
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _descriptors;
        
        public abstract DiagnosticDescriptor Descriptor { get;  }

        protected ImmutableHashSet<string> RequiredTypeNames { get; private set; } = ImmutableHashSet<string>.Empty;
        protected ImmutableHashSet<string> TypeNames { get; private set; } = ImmutableHashSet<string>.Empty;

        protected SingleSharedDiagnosticAnalyzer()
        {
            Setup();
        }

        public override void Initialize(AnalysisContext context)
        {
            PreInitialize(context);
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                                   GeneratedCodeAnalysisFlags.ReportDiagnostics);

            if (RequiredTypeNames.IsEmpty && TypeNames.IsEmpty)
            {
                HandleContext(context);
            }
            else
            {
                context.RegisterCompilationStartAction(ctx =>
                {
                    var results = RequiredTypeNames
                        .Select(type => (TypeName: type, Type : ctx.Compilation.GetTypeByMetadataName(type)))
                        .ToArray();
                    if (results.All(x => x.Type != null))
                    {
                        results = results.Concat(TypeNames.Select(type =>
                            (TypeName: type, Type: ctx.Compilation.GetTypeByMetadataName(type)))).ToArray();

                        HandleStartCompilationContext(results.ToDictionary(x => x.TypeName, x => x.Type), ctx);
                    }
                });
            }

            PostInitialize(context);
        }

        protected virtual void HandleContext(AnalysisContext analysisContext)
        {
            throw new NotImplementedException("This method should be handled");
        }

        protected virtual void HandleStartCompilationContext(Dictionary<string,INamedTypeSymbol> types,
            CompilationStartAnalysisContext context)
        {
            throw new NotImplementedException("This method should be handled");
        }

        protected virtual void PostInitialize(AnalysisContext context)
        {
            
        }

        protected virtual void PreInitialize(AnalysisContext context)
        {
            
        }

        protected void Setup()
        {
            _descriptors = ImmutableArray<DiagnosticDescriptor>.Empty.Add(Descriptor);
            DoSetup();
        }

        protected abstract void DoSetup();

        protected void AddType(string typeName, bool required)
        {
            if (required)
            {
                RequiredTypeNames = RequiredTypeNames.Add(typeName);
            }
            else
            {
                TypeNames = TypeNames.Add(typeName);
            }
            
        }

        

        protected void AddGenericType(string typeName, int typeParametersCount, bool required)
        {
            AddType($"{typeName}`{typeParametersCount}", required);
        }

    }
}
