// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class UseConcurrencyLimiterAnalyzer
    {
        private readonly StartupAnalysis _context;

        public UseConcurrencyLimiterAnalyzer(StartupAnalysis context)
        {
            _context = context;
        }
        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            Debug.Assert(context.Symbol.Kind == SymbolKind.NamedType);
            Debug.Assert(StartupFacts.IsStartupClass(_context.StartupSymbols, (INamedTypeSymbol)context.Symbol));

            var type = (INamedTypeSymbol)context.Symbol;

            foreach (var middlewareAnalysis in _context.GetRelatedAnalyses<MiddlewareAnalysis>(type))
            {
                MiddlewareItem? useConcurrencyLimiterItem = default;
                MiddlewareItem? useRoutingItem = default;
                MiddlewareItem? useEndpoint = default;

                var length = middlewareAnalysis.Middleware.Length;
                for (var i = length - 1; i >= 0; i--)
                {
                    var middlewareItem = middlewareAnalysis.Middleware[i];
                    var middleware = middlewareItem.UseMethod.Name;

                    if (middleware == "UseConcurrencyLimiter")
                    {
                        if (useRoutingItem != null && useConcurrencyLimiterItem == null)
                        {
                            // This looks like
                            //
                            //  app.UseConcurrencyLimiter();
                            //  ...
                            //  app.UseRouting();
                            //  app.UseEndpoints(...);

                            context.ReportDiagnostic(Diagnostic.Create(
                                StartupAnalyzer.Diagnostics.IncorrectlyConfiguredConcurrencyLimiterMiddleware,
                                middlewareItem.Operation.Syntax.GetLocation(),
                                middlewareItem.UseMethod.Name));
                        }

                        useConcurrencyLimiterItem = middlewareItem;
                    }
                    else if (middleware == "UseEndpoints")
                    {
                        if (useConcurrencyLimiterItem != null)
                        {
                            // This configuration looks like
                            //
                            //  app.UseRouting();
                            //  app.UseEndpoints(...);
                            //  ...
                            //  app.UseConcurrencyLimiter();
                            //

                            context.ReportDiagnostic(Diagnostic.Create(
                                StartupAnalyzer.Diagnostics.IncorrectlyConfiguredConcurrencyLimiterMiddleware,
                                useConcurrencyLimiterItem.Operation.Syntax.GetLocation(),
                                middlewareItem.UseMethod.Name));
                        }

                        useEndpoint = middlewareItem;
                    }
                    else if (middleware == "UseRouting")
                    {
                        if (useEndpoint is null)
                        {
                            // We're likely here because the middleware uses an expression chain e.g.
                            // app.UseRouting()
                            //   .UseConcurrencyLimiter()
                            //   .UseEndpoints(..));
                            // This analyzer expects MiddlewareItem instances to appear in the order in which they appear in source
                            // which unfortunately isn't true for chained calls (the operations appear in reverse order).
                            // We'll avoid doing any analysis in this event and rely on the runtime guardrails.
                            // We'll use https://github.com/dotnet/aspnetcore/issues/16648 to track addressing this in a future milestone
                            return;
                        }

                        useRoutingItem = middlewareItem;
                    }
                }
            }
        }
    }
}
