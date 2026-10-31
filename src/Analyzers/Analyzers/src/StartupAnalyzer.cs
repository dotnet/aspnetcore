// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class StartupAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var symbols = new StartupSymbols(context.Compilation);

        // Don't run analyzer if ASP.NET Core types cannot be found
        if (!symbols.HasRequiredSymbols)
        {
            return;
        }

        var entryPoint = context.Compilation.GetEntryPoint(context.CancellationToken);

        context.RegisterSymbolStartAction(context =>
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (!StartupFacts.IsStartupClass(symbols, type) && !SymbolEqualityComparer.Default.Equals(entryPoint?.ContainingType, type))
            {
                // Not a startup class, nothing to do.
                return;
            }

            // This analyzer fans out a bunch of jobs. The context will capture the results of doing analysis
            // on the startup code, so that other analyzers that run later can examine them.
            var builder = new StartupAnalysisBuilder(this, symbols);

            var services = new ServicesAnalyzer(builder);
            var options = new OptionsAnalyzer(builder);
            var middleware = new MiddlewareAnalyzer(builder);

            context.RegisterOperationBlockStartAction(context =>
            {
                if (context.OwningSymbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                var method = (IMethodSymbol)context.OwningSymbol;
                var isConfigureServices = StartupFacts.IsConfigureServices(symbols, method);
                if (isConfigureServices)
                {
                    OnConfigureServicesMethodFound(method);
                }

                // In the future we can consider looking at more methods, but for now limit to Main, implicit Main, and Configure* methods
                var isMain = SymbolEqualityComparer.Default.Equals(entryPoint, context.OwningSymbol);

                if (isConfigureServices || isMain)
                {
                    services.AnalyzeConfigureServices(context);
                    options.AnalyzeConfigureServices(context);
                }

                var isConfigure = StartupFacts.IsConfigure(symbols, method);
                if (isConfigure)
                {
                    OnConfigureMethodFound(method);
                }
                if (isConfigure || isMain)
                {
                    middleware.AnalyzeConfigureMethod(context);
                }
            });

            // Run after analyses have had a chance to finish to add diagnostics.
            context.RegisterSymbolEndAction(context =>
        {
            var analysis = builder.Build();
            new UseMvcAnalyzer(analysis).AnalyzeSymbol(context);
            new BuildServiceProviderAnalyzer(analysis).AnalyzeSymbol(context);
            new UseAuthorizationAnalyzer(analysis).AnalyzeSymbol(context);
        });

        }, SymbolKind.NamedType);
    }
}
