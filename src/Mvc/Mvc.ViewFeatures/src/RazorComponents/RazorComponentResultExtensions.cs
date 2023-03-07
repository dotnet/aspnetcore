// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Holds extension methods on <see cref="IResultExtensions"/>.
/// </summary>
public static class RazorComponentResultExtensions
{
    /// <summary>
    /// Returns an <see cref="IActionResult"/> that renders a Razor Component.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to render.</typeparam>
    /// <param name="resultExtensions">The <see cref="ResultExtensions"/>.</param>
    /// <returns>An <see cref="IActionResult"/> that renders a Razor Component.</returns>
    public static IActionResult RazorComponent<TComponent>(this IResultExtensions resultExtensions) where TComponent : IComponent
        => new RazorComponentResult(typeof(TComponent));

    /// <summary>
    /// Returns an <see cref="IActionResult"/> that renders a Razor Component.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to render.</typeparam>
    /// <param name="resultExtensions">The <see cref="ResultExtensions"/>.</param>
    /// <param name="componentParameters">Parameters for the component.</param>
    /// <returns>An <see cref="IActionResult"/> that renders a Razor Component.</returns>
    public static IActionResult RazorComponent<TComponent>(this IResultExtensions resultExtensions, object componentParameters) where TComponent : IComponent
        => new RazorComponentResult(typeof(TComponent)) { Parameters = componentParameters };

    /// <summary>
    /// Returns an <see cref="IActionResult"/> that renders a Razor Component.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to render.</typeparam>
    /// <param name="resultExtensions">The <see cref="ResultExtensions"/>.</param>
    /// <param name="componentParameters">Parameters for the component.</param>
    /// <param name="renderMode">A <see cref="RenderMode"/> value that specifies how to render the component.</param>
    /// <returns>An <see cref="IActionResult"/> that renders a Razor Component.</returns>
    public static IActionResult RazorComponent<TComponent>(this IResultExtensions resultExtensions, RenderMode renderMode, object componentParameters) where TComponent: IComponent
        => new RazorComponentResult(typeof(TComponent)) { RenderMode = renderMode, Parameters = componentParameters };
}
