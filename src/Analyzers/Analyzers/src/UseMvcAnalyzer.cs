// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class UseMvcAnalyzer
    {
        private readonly StartupAnalysis _context;

        public UseMvcAnalyzer(StartupAnalysis context)
        {
            _context = context;
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            Debug.Assert(context.Symbol.Kind == SymbolKind.NamedType);
            Debug.Assert(StartupFacts.IsStartupClass(_context.StartupSymbols, (INamedTypeSymbol)context.Symbol));

            var type = (INamedTypeSymbol)context.Symbol;

            var optionsAnalysis = _context.GetRelatedSingletonAnalysis<OptionsAnalysis>(type);
            if (optionsAnalysis == null)
            {
                return;
            }

            // Find the middleware analysis foreach of the Configure methods defined by this class and validate.
            //
            // Note that this doesn't attempt to handle inheritance scenarios.
            foreach (var middlewareAnalysis in _context.GetRelatedAnalyses<MiddlewareAnalysis>(type))
            {
                foreach (var middlewareItem in middlewareAnalysis.Middleware)
                {
                    if (middlewareItem.UseMethod.Name == "UseMvc" || middlewareItem.UseMethod.Name == "UseMvcWithDefaultRoute")
                    {
                        // Report a diagnostic if it's unclear that the user turned off Endpoint Routing.
                        if (!OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting,
                                middlewareItem.Operation.Syntax.GetLocation(),
                                middlewareItem.UseMethod.Name,
                                optionsAnalysis.ConfigureServicesMethod.Name));
                        }
                    }
                }
            }
        }
    }
}
