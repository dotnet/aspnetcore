// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class BuildServiceProviderValidator : StartupDiagnosticValidator
    {
        public static BuildServiceProviderValidator CreateAndInitialize(CompilationAnalysisContext context, ConcurrentBag<StartupComputedAnalysis> analyses)
        {
            if (analyses == null)
            {
                throw new ArgumentNullException(nameof(analyses));
            }

            var validator = new BuildServiceProviderValidator();

            foreach (var serviceAnalysis in analyses.OfType<ServicesAnalysis>())
            {
                foreach (var serviceItem in serviceAnalysis.Services)
                {
                    if (serviceItem.UseMethod.Name == "BuildServiceProvider")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            StartupAnalzyer.BuildServiceProviderShouldNotCalledInConfigureServicesMethod,
                            serviceItem.Operation.Syntax.GetLocation(),
                            serviceItem.UseMethod.Name,
                            serviceAnalysis.ConfigureServicesMethod.Name));
                    }
                }
            }

            return validator;
        }
    }
}
