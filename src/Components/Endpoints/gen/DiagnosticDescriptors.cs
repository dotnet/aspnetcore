// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "BlazorNativeAot";

    // A component was found but could not be described end to end. Reported rather than silently
    // skipped, because the runtime's fallback for an undescribed component is reflection, which is
    // exactly what a Native AOT application is trying to avoid.
    public static readonly DiagnosticDescriptor ComponentNotFullyDescribed = new(
        id: "BLAZORAOT001",
        title: "Component cannot be described completely",
        messageFormat: "'{0}' cannot be described for Native AOT because {1}. The framework will reflect over it at runtime.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The Blazor Native AOT metadata generator only describes a component when every member it needs is reachable from the generated code.");

    // A [BindableModel] named something the generator cannot walk, so any @bind expression rooted at
    // it falls back to the MemberInfo walk.
    public static readonly DiagnosticDescriptor BindableModelNotDescribed = new(
        id: "BLAZORAOT002",
        title: "Form model cannot be described completely",
        messageFormat: "'{0}' cannot be described for Native AOT because {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The Blazor Native AOT metadata generator describes a form model and every type reachable from it, so that a binding expression can be walked instead of compiled.");

    // The metadata context itself is malformed; nothing can be generated for it.
    public static readonly DiagnosticDescriptor MetadataContextMustBePartial = new(
        id: "BLAZORAOT003",
        title: "Metadata context declaration must be partial",
        messageFormat: "'{0}' must be declared partial so the generator can emit the metadata context declaration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The generator implements the metadata context and reopens each of its containing types in a second partial declaration.");

    // Endpoint behavior reads arbitrary attributes off the component. Report any attribute that cannot
    // be reconstructed so strict metadata mode never silently drops authorization or caching policy.
    public static readonly DiagnosticDescriptor ComponentAttributeNotDescribed = new(
        id: "BLAZORAOT004",
        title: "Component attribute cannot be described",
        messageFormat: "The '{1}' on '{0}' cannot be described for Native AOT and will not be applied without the reflection fallback. Make the attribute type and constructor accessible and use reconstructable attribute arguments.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Endpoint-visible component attributes must be reconstructable so generated metadata preserves routing, authorization, caching, and rendering behavior.");

}
