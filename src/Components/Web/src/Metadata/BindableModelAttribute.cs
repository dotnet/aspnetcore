// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Names a form model type, from which the Blazor Native AOT metadata source generator derives the
/// whole bindable graph. Applied to a <see cref="RazorComponentsMetadataContext"/>, once per model.
/// </summary>
/// <remarks>
/// <para>
/// The generator emits a <see cref="Components.Infrastructure.BindableTypeDescriptor"/> for the named
/// type and for every type reachable from it through instance fields, instance properties and
/// single-argument indexers, stopping at framework primitives. One attribute per <c>EditForm</c> model
/// therefore covers every expression that form can produce.
/// </para>
/// <para>
/// Naming the type explicitly is a requirement rather than a convenience: source generators all run
/// against the same input compilation and cannot observe each other's output, so the Razor-generated
/// component types are not in the compilation this generator sees. Models are declared in ordinary C#
/// and are visible, which is why the attribute names the model rather than the component.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class BindableModelAttribute : Attribute
{
    /// <summary>
    /// Gets the form model type to describe.
    /// </summary>
    public required Type ModelType { get; init; }
}
