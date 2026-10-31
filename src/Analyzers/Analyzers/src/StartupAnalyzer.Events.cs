// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers;

// Events for testability. Allows us to unit test the data we gather from analysis.
public partial class StartupAnalyzer : DiagnosticAnalyzer
{
    internal event EventHandler<IMethodSymbol>? ConfigureServicesMethodFound;

    internal void OnConfigureServicesMethodFound(IMethodSymbol method)
    {
        ConfigureServicesMethodFound?.Invoke(this, method);
    }

    internal event EventHandler<ServicesAnalysis>? ServicesAnalysisCompleted;

    internal void OnServicesAnalysisCompleted(ServicesAnalysis analysis)
    {
        ServicesAnalysisCompleted?.Invoke(this, analysis);
    }

    internal event EventHandler<OptionsAnalysis>? OptionsAnalysisCompleted;

    internal void OnOptionsAnalysisCompleted(OptionsAnalysis analysis)
    {
        OptionsAnalysisCompleted?.Invoke(this, analysis);
    }

    internal event EventHandler<IMethodSymbol>? ConfigureMethodFound;

    internal void OnConfigureMethodFound(IMethodSymbol method)
    {
        ConfigureMethodFound?.Invoke(this, method);
    }

    internal event EventHandler<MiddlewareAnalysis>? MiddlewareAnalysisCompleted;

    internal void OnMiddlewareAnalysisCompleted(MiddlewareAnalysis analysis)
    {
        MiddlewareAnalysisCompleted?.Invoke(this, analysis);
    }
}
