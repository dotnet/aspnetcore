// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    public partial class StartupAnalzyer : DiagnosticAnalyzer
    {
        internal event EventHandler<StartupComputedAnalysis> AnalysisStarted;

        private void OnAnalysisStarted(StartupComputedAnalysis analysis)
        {
            AnalysisStarted?.Invoke(this, analysis);
        }

        internal event EventHandler<IMethodSymbol> ConfigureServicesMethodFound;

        private void OnConfigureServicesMethodFound(IMethodSymbol method)
        {
            ConfigureServicesMethodFound?.Invoke(this, method);
        }

        internal event EventHandler<IMethodSymbol> ConfigureMethodFound;

        private void OnConfigureMethodFound(IMethodSymbol method)
        {
            ConfigureMethodFound?.Invoke(this, method);
        }
    }
}
