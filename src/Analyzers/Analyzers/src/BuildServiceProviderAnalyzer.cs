// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class BuildServiceProviderValidator
    {
        private readonly StartupAnalysis _context;

        public BuildServiceProviderValidator(StartupAnalysis context)
        {
            _context = context;
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            Debug.Assert(context.Symbol.Kind == SymbolKind.NamedType);
            Debug.Assert(StartupFacts.IsStartupClass(_context.StartupSymbols, (INamedTypeSymbol)context.Symbol));

            var type = (INamedTypeSymbol)context.Symbol;

            foreach (var serviceAnalysis in _context.GetRelatedAnalyses<ServicesAnalysis>(type))
            {
                foreach (var serviceItem in serviceAnalysis.Services)
                {
                    if (serviceItem.UseMethod.Name == "BuildServiceProvider")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            StartupAnalyzer.Diagnostics.BuildServiceProviderShouldNotCalledInConfigureServicesMethod,
                            serviceItem.Operation.Syntax.GetLocation(),
                            serviceItem.UseMethod.Name,
                            serviceAnalysis.ConfigureServicesMethod.Name));
                    }
                }
            }
        }
    }
}
