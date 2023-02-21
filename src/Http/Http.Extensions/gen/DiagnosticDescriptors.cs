// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators;

internal static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor UnableToResolveRoutePattern { get; } = new(
        "RDG001",
        new LocalizableResourceString(nameof(Resources.UnableToResolveRoutePattern_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.UnableToResolveRoutePattern_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor UnableToResolveMethod { get; } = new(
        "RDG002",
        new LocalizableResourceString(nameof(Resources.UnableToResolveMethod_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.UnableToResolveMethod_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    // This is temporary. The plan is to be able to resolve all parameters to a known EndpointParameterSource.
    public static DiagnosticDescriptor GetUnableToResolveParameterDescriptor(string parameterName)
    {
        return new(
            "RDG073",
            new LocalizableResourceString(nameof(Resources.UnableToResolveParameter_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.FormatUnableToResolveParameter_Message), Resources.ResourceManager, typeof(Resources), parameterName),
            "Usage",
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true);
    }
}
