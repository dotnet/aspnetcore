// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Provides methods to instantiate and release a ViewComponent.
/// </summary>
public interface IViewComponentActivator
{
    /// <summary>
    /// Instantiates a ViewComponent.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ViewComponentContext"/> for the executing <see cref="ViewComponent"/>.
    /// </param>
    object Create(ViewComponentContext context);

    /// <summary>
    /// Releases a ViewComponent instance.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ViewComponentContext"/> associated with the <paramref name="viewComponent"/>.
    /// </param>
    /// <param name="viewComponent">The <see cref="ViewComponent"/> to release.</param>
    void Release(ViewComponentContext context, object viewComponent);

    /// <summary>
    /// Releases a ViewComponent instance.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ViewComponentContext"/> associated with the <paramref name="viewComponent"/>.
    /// </param>
    /// <param name="viewComponent">The <see cref="ViewComponent"/> to release.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the view component has been disposed.</returns>
    ValueTask ReleaseAsync(ViewComponentContext context, object viewComponent)
    {
        Release(context, viewComponent);
        return default;
    }
}
