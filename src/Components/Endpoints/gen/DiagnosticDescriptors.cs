// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "BlazorNativeAot";

    public static readonly DiagnosticDescriptor BindableModelNotDescribed = new(
        id: "BLAZORAOT002",
        title: "Form model cannot be described completely",
        messageFormat: "'{0}' cannot be described for Native AOT because {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The Blazor Native AOT metadata generator describes a form model and every type reachable from it, so that a binding expression can be walked instead of compiled.");

    public static readonly DiagnosticDescriptor MetadataContextMustBePartial = new(
        id: "BLAZORAOT003",
        title: "Metadata context declaration must be partial",
        messageFormat: "'{0}' must be declared partial so the generator can emit the metadata context declaration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The generator implements the metadata context and reopens each of its containing types in a second partial declaration.");
}
