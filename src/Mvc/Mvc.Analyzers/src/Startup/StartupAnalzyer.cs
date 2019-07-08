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
        private readonly static Func<StartupAnalysisContext, StartupComputedAnalysis>[] ConfigureServicesMethodAnalysisFactories = new Func<StartupAnalysisContext, StartupComputedAnalysis>[]
        {
            ServicesAnalysis.CreateAndInitialize,
            MvcOptionsAnalysis.CreateAndInitialize,
        };

        private readonly static Func<StartupAnalysisContext, StartupComputedAnalysis>[] ConfigureMethodAnalysisFactories = new Func<StartupAnalysisContext, StartupComputedAnalysis>[]
        {
            MiddlewareAnalysis.CreateAndInitialize,
        };

        private readonly static Func<CompilationAnalysisContext, ConcurrentBag<StartupComputedAnalysis>, StartupDiagnosticValidator>[] DiagnosticValidatorFactories = new Func<CompilationAnalysisContext, ConcurrentBag<StartupComputedAnalysis>, StartupDiagnosticValidator>[]
        {
            UseMvcDiagnosticValidator.CreateAndInitialize,
            BuildServiceProviderValidator.CreateAndInitialize
        };

#pragma warning restore RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.

        public StartupAnalzyer()
        {
            SupportedDiagnostics = ImmutableArray.Create<DiagnosticDescriptor>(new[]
            {
                UnsupportedUseMvcWithEndpointRouting,
                BuildServiceProviderShouldNotCalledInConfigureServicesMethod
            });

            // By default the analyzer will only run for files ending with Startup.cs
            // Can be overriden for unit testing other file names
            // Analzyer only runs for C# so limiting to *.cs file is fine
            StartupFilePredicate = path => path.EndsWith("Startup.cs", StringComparison.OrdinalIgnoreCase);
        }

        internal Func<string, bool> StartupFilePredicate { get; set; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var symbols = new StartupSymbols(context.Compilation);

            // Don't run analyzer if ASP.NET Core types cannot be found
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
                AnalyzeStartupMethods(context, symbols, analyses);
            });

            // Run after analyses have had a chance to finish to add diagnostics.
            context.RegisterCompilationEndAction(analysisContext =>
            {
                RunAnalysis(analysisContext, analyses);
            });
        }

        private static void RunAnalysis(CompilationAnalysisContext analysisContext, ConcurrentBag<StartupComputedAnalysis> analyses)
        {
            for (var i = 0; i < DiagnosticValidatorFactories.Length; i++)
            {
                var validator = DiagnosticValidatorFactories[i].Invoke(analysisContext, analyses);
            }
        }

        private void AnalyzeStartupMethods(OperationBlockStartAnalysisContext context, StartupSymbols symbols, ConcurrentBag<StartupComputedAnalysis> analyses)
        {
            if (!IsStartupFile(context))
            {
                return;
            }

            if (context.OwningSymbol.Kind != SymbolKind.Method)
            {
                return;
            }

            var startupAnalysisContext = new StartupAnalysisContext(context, symbols);

            var method = (IMethodSymbol)context.OwningSymbol;
            if (StartupFacts.IsConfigureServices(symbols, method))
            {
                for (var i = 0; i < ConfigureServicesMethodAnalysisFactories.Length; i++)
                {
                    var analysis = ConfigureServicesMethodAnalysisFactories[i].Invoke(startupAnalysisContext);
                    analyses.Add(analysis);

                    OnAnalysisStarted(analysis);
                }

                OnConfigureServicesMethodFound(method);
            }

            if (StartupFacts.IsConfigure(symbols, method))
            {
                for (var i = 0; i < ConfigureMethodAnalysisFactories.Length; i++)
                {
                    var analysis = ConfigureMethodAnalysisFactories[i].Invoke(startupAnalysisContext);
                    analyses.Add(analysis);

                    OnAnalysisStarted(analysis);
                }

                OnConfigureMethodFound(method);
            }
        }
#pragma warning disable RS1012 // Start action has no registered actions.
        private bool IsStartupFile(OperationBlockStartAnalysisContext context)
        {
            foreach (var location in context.OwningSymbol.Locations)
            {
                if (location.IsInSource && StartupFilePredicate(location.SourceTree.FilePath))
                {
                    return true;
                }
            }

            return false;
        }
#pragma warning restore RS1012 // Start action has no registered actions.
    }
}
