// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public partial class StartupAnalzyer : DiagnosticAnalyzer
    {
#pragma warning disable RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.
        private readonly static Func<OperationBlockStartAnalysisContext, StartupComputedAnalysis>[] ConfigureServicesMethodAnalysisFactories = new Func<OperationBlockStartAnalysisContext, StartupComputedAnalysis>[]
        {
            ServicesAnalysis.CreateAndInitialize,
            MvcOptionsAnalysis.CreateAndInitialize,
        };

        private readonly static Func<OperationBlockStartAnalysisContext, StartupComputedAnalysis>[] ConfigureMethodAnalysisFactories = new Func<OperationBlockStartAnalysisContext, StartupComputedAnalysis>[]
        {
            MiddlewareAnalysis.CreateAndInitialize,
        };

        private readonly static Func<SemanticModelAnalysisContext, ConcurrentBag<StartupComputedAnalysis>, StartupDiagnosticValidator>[] DiagnosticValidatorFactories = new Func<SemanticModelAnalysisContext, ConcurrentBag<StartupComputedAnalysis>, StartupDiagnosticValidator>[]
        {
            UseMvcDiagnosticValidator.CreateAndInitialize,
            MiddlewareRequiredServiceValidator.CreateAndInitialize,
            MiddlewareOrderingValidator.CreateAndInitialize,
        };

#pragma warning restore RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.

        public StartupAnalzyer()
        {
            SupportedDiagnostics = ImmutableArray.Create<DiagnosticDescriptor>(new[]
            {
                MiddlewareMissingRequiredServices,
                MiddlewareInvalidOrder,
                UnsupportedUseMvcWithEndpointRouting,
            });
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var symbols = new StartupSymbols(context.Compilation);
            if (symbols.IServiceCollection == null || symbols.IApplicationBuilder == null)
            {
                return;
            }

            // This analyzer is a general-purpose framework that functions by:
            // 1. Discovering Startup methods (ConfigureServices, Configure)
            // 2. Launching additional analyses of these Startup methods and collecting results
            // 3. Running a final pass to add diagnostics based on computed state
            var analyses = new ConcurrentBag<StartupComputedAnalysis>();

            context.RegisterOperationBlockStartAction(context =>
            {
                if (context.OwningSymbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                var method = (IMethodSymbol)context.OwningSymbol;
                if (StartupFacts.IsConfigureServices(symbols, method))
                {
                    for (var i = 0; i < ConfigureServicesMethodAnalysisFactories.Length; i++)
                    {
                        var analysis = ConfigureServicesMethodAnalysisFactories[i].Invoke(context);
                        analyses.Add(analysis);

                        OnAnalysisStarted(analysis);
                    }

                    OnConfigureServicesMethodFound(method);
                }

                if (StartupFacts.IsConfigure(symbols, method))
                {
                    for (var i = 0; i < ConfigureMethodAnalysisFactories.Length; i++)
                    {
                        var analysis = ConfigureMethodAnalysisFactories[i].Invoke(context);
                        analyses.Add(analysis);

                        OnAnalysisStarted(analysis);
                    }

                    OnConfigureMethodFound(method);
                }

            });

            // Run after analyses have had a chance to finish to add diagnostics.
            context.RegisterSemanticModelAction(semanticModelContext =>
            {
                for (var i = 0; i < DiagnosticValidatorFactories.Length; i++)
                {
                    var validator = DiagnosticValidatorFactories[i].Invoke(semanticModelContext, analyses);
                }
            });
        }
    }
}
