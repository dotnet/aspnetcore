// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers;

public partial class StartupAnalyzer : DiagnosticAnalyzer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    internal static class Diagnostics
    {
        internal static readonly DiagnosticDescriptor BuildServiceProviderShouldNotCalledInConfigureServicesMethod = new DiagnosticDescriptor(
            "ASP0000",
            "Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'",
            "Calling 'BuildServiceProvider' from application code results in an additional copy of singleton services being created. Consider alternatives such as dependency injecting services as parameters to 'Configure'.",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/AA5k895");

        internal static readonly DiagnosticDescriptor UnsupportedUseMvcWithEndpointRouting = new DiagnosticDescriptor(
            "MVC1005",
            "Cannot use UseMvc with Endpoint Routing",
            "Using '{0}' to configure MVC is not supported while using Endpoint Routing. To continue using '{0}', please set 'MvcOptions.EnableEndpointRouting = false' inside '{1}'.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/YJggeFn");

        internal static readonly DiagnosticDescriptor IncorrectlyConfiguredAuthorizationMiddleware = new DiagnosticDescriptor(
            "ASP0001",
            "Authorization middleware is incorrectly configured",
            "The call to UseAuthorization should appear between app.UseRouting() and app.UseEndpoints(..) for authorization to be correctly evaluated",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/AA64fv1");

        public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = ImmutableArray.Create<DiagnosticDescriptor>(new[]
        {
            // ASP
            BuildServiceProviderShouldNotCalledInConfigureServicesMethod,
            IncorrectlyConfiguredAuthorizationMiddleware,

            // MVC
            UnsupportedUseMvcWithEndpointRouting,
        });
    }
}
