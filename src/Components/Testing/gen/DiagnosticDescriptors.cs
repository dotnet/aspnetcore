// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Testing.Generators;

internal static class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor MethodNotFound = new(
        "E2E001",
        "Service override method not found",
        "Method '{0}' with parameter '(IServiceCollection)' was not found on type '{1}'",
        "Microsoft.AspNetCore.Components.Testing",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NonConstantMethodName = new(
        "E2E002",
        "Non-constant method name in ConfigureServices",
        "The method name argument to ConfigureServices must be a compile-time constant (string literal or nameof). The override will fall back to reflection at runtime.",
        "Microsoft.AspNetCore.Components.Testing",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MethodMustBeStatic = new(
        "E2E003",
        "Service override method must be static",
        "Method '{0}' on type '{1}' must be static to be used as a service override",
        "Microsoft.AspNetCore.Components.Testing",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
