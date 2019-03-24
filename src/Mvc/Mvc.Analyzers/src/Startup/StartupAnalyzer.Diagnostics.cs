// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    public partial class StartupAnalzyer : DiagnosticAnalyzer
    {
        internal readonly static DiagnosticDescriptor MiddlewareMissingRequiredServices = new DiagnosticDescriptor(
            "ASPC1000",
            "Missing required services.",
            "The middleware created by '{0}' is missing required services. To add the services, place a call to '{1}' inside '{2}'.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: null); // TODO docs link.

        internal readonly static DiagnosticDescriptor MiddlewareInvalidOrder = new DiagnosticDescriptor(
            "ASPC1001",
            "Invalid middleware order.",
            "The middleware created by '{0}' and '{1}' have an unsatisfied order dependency. To correct this, place the call to '{0}' before '{1}'.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: null); // TODO docs link.

        internal readonly static DiagnosticDescriptor UnsupportedUseMvcWithEndpointRouting = new DiagnosticDescriptor(
            "MVC1005", 
            "Cannot use UseMvc with Endpoint Routing.", 
            "Using '{0}' to configure MVC is not supported while using Endpoint Routing. To continue using '{0}', please set 'MvcOptions.EnableEndpointRounting = false' inside '{1}'.",
            "Usage", 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true,
            helpLinkUri: null); // TODO docs link.
    }
}
