// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Provides methods for creation and disposal of view components.
/// </summary>
public interface IViewComponentFactory
{
    /// <summary>
    /// Creates a new controller for the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context"><see cref="ViewComponentContext"/> for the view component.</param>
    /// <returns>The view component.</returns>
    object CreateViewComponent(ViewComponentContext context);

    /// <summary>
    /// Releases a view component instance.
    /// </summary>
    /// <param name="context">The context associated with the <paramref name="component"/>.</param>
    /// <param name="component">The view component.</param>
    void ReleaseViewComponent(ViewComponentContext context, object component);

    /// <summary>
    /// Releases a view component instance asynchronously.
    /// </summary>
    /// <param name="context">The context associated with the <paramref name="component"/>.</param>
    /// <param name="component">The view component.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the view component has been released.</returns>
    ValueTask ReleaseViewComponentAsync(ViewComponentContext context, object component)
    {
        ReleaseViewComponent(context, component);
        return default;
    }
}
