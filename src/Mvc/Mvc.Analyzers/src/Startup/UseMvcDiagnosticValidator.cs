// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class UseMvcDiagnosticValidator : StartupDiagnosticValidator
    {
        public static UseMvcDiagnosticValidator CreateAndInitialize(CompilationAnalysisContext context, ConcurrentBag<StartupComputedAnalysis> analyses)
        {
            if (analyses == null)
            {
                throw new ArgumentNullException(nameof(analyses));
            }

            var validator = new UseMvcDiagnosticValidator();

            foreach (var mvcOptionsAnalysis in analyses.OfType<MvcOptionsAnalysis>())
            {
                // Each analysis of the options is one-per-class (in user code). Find the middleware analysis foreach of the Configure methods
                // defined by this class and validate.
                //
                // Note that this doesn't attempt to handle inheritance scenarios.
                foreach (var middlewareAnalsysis in analyses.OfType<MiddlewareAnalysis>().Where(m => m.EnclosingType == mvcOptionsAnalysis.EnclosingType))
                {
                    foreach (var middlewareItem in middlewareAnalsysis.Middleware)
                    {
                        if ((middlewareItem.UseMethod.Name == "UseMvc" || middlewareItem.UseMethod.Name == "UseMvcWithDefaultRoute") &&

                            // Report a diagnostic if it's unclear that the user turned off Endpoint Routing.
                            (mvcOptionsAnalysis.EndpointRoutingEnabled == true || mvcOptionsAnalysis.EndpointRoutingEnabled == null))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                StartupAnalzyer.UnsupportedUseMvcWithEndpointRouting,
                                middlewareItem.Operation.Syntax.GetLocation(),
                                middlewareItem.UseMethod.Name,
                                mvcOptionsAnalysis.ConfigureServicesMethod.Name));
                        }
                    }
                }
            }

            return validator;
        }
    }
}
