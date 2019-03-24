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
    internal class MiddlewareRequiredServiceValidator : StartupDiagnosticValidator
    {
        private readonly static ImmutableDictionary<string, ImmutableArray<string>> MiddlewareMap = ImmutableDictionary.CreateRange<string, ImmutableArray<string>>(new[]
        {
            new KeyValuePair<string, ImmutableArray<string>>("UseHealthChecks", ImmutableArray.Create<string>(new[]
            {
                "AddHealthChecks",
            })),
        });

        private readonly static ImmutableDictionary<string, ImmutableArray<string>> ServicesMap = ImmutableDictionary.CreateRange<string, ImmutableArray<string>>(new[]
        {
            new KeyValuePair<string, ImmutableArray<string>>("AddMvc", ImmutableArray.Create<string>(new[]
            {
                "AddRouting",
            })),
        });

        public static MiddlewareRequiredServiceValidator CreateAndInitialize(SemanticModelAnalysisContext context, ConcurrentBag<StartupComputedAnalysis> analyses)
        {
            if (analyses == null)
            {
                throw new ArgumentNullException(nameof(analyses));
            }

            var validator = new MiddlewareRequiredServiceValidator();

            foreach (var servicesAnalysis in analyses.OfType<ServicesAnalysis>())
            {

                var occluded = new HashSet<string>();
                foreach (var entry in servicesAnalysis.Services)
                {
                    occluded.Add(entry.UseMethod.Name);

                    if (ServicesMap.TryGetValue(entry.UseMethod.Name, out var additional))
                    {
                        foreach (var item in additional)
                        {
                            occluded.Add(item);
                        }
                    }
                }

                // Each analysis of the options is one-per-class (in user code). Find the middleware analysis foreach of the Configure methods
                // defined by this class and validate.
                //
                // Note that this doesn't attempt to handle inheritance scenarios.
                foreach (var middlewareAnalsysis in analyses.OfType<MiddlewareAnalysis>().Where(m => m.EnclosingType == servicesAnalysis.EnclosingType))
                {
                    foreach (var middlewareItem in middlewareAnalsysis.Middleware)
                    {
                        if (MiddlewareMap.TryGetValue(middlewareItem.UseMethod.Name, out var requiredServices))
                        {
                            foreach (var requiredService in requiredServices)
                            {
                                if (!occluded.Contains(requiredService))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(
                                        StartupAnalzyer.MiddlewareMissingRequiredServices,
                                        middlewareItem.Operation.Syntax.GetLocation(),
                                        middlewareItem.UseMethod.Name,
                                        requiredService,
                                        servicesAnalysis.ConfigureServicesMethod.Name));
                                }
                            }
                        }
                    }
                }
            }

            return validator;
        }
    }
}
