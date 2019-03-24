// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class MiddlewareOrderingValidator : StartupDiagnosticValidator
    {
        // This should probably be a multi-map, but oh-well.
        private readonly static ImmutableDictionary<string, string> MiddlewareHappensAfterMap = ImmutableDictionary.CreateRange<string, string>(new[]
        {
            new KeyValuePair<string, string>("UseAuthorization", "UseAuthentication"),
        });

        public static MiddlewareRequiredServiceValidator CreateAndInitialize(SemanticModelAnalysisContext context, ConcurrentBag<StartupComputedAnalysis> analyses)
        {
            if (analyses == null)
            {
                throw new ArgumentNullException(nameof(analyses));
            }

            var validator = new MiddlewareRequiredServiceValidator();

            // Note: this is a simple source-order implementation. We don't attempt perform data flow
            // analysis in order to determine the actual order in which middleware are ordered.
            //
            // This can currently be confused by things like Map(...)
            foreach (var middlewareAnalsysis in analyses.OfType<MiddlewareAnalysis>())
            {
                for (var i = 0; i < middlewareAnalsysis.Middleware.Length; i++)
                {
                    var middlewareItem = middlewareAnalsysis.Middleware[i];
                    if (MiddlewareHappensAfterMap.TryGetValue(middlewareItem.UseMethod.Name, out var cannotComeAfter))
                    {
                        for (var j = i; j < middlewareAnalsysis.Middleware.Length; j++)
                        {
                            var candidate = middlewareAnalsysis.Middleware[j];
                            if (string.Equals(cannotComeAfter, candidate.UseMethod.Name, StringComparison.Ordinal))
                            {
                                // Found the other middleware after current one. This is an error.
                                context.ReportDiagnostic(Diagnostic.Create(
                                    StartupAnalzyer.MiddlewareInvalidOrder,
                                    candidate.Operation.Syntax.GetLocation(),
                                    middlewareItem.UseMethod.Name,
                                    candidate.UseMethod.Name));
                            }
                        }
                    }
                }
            }

            return validator;
        }
    }
}
