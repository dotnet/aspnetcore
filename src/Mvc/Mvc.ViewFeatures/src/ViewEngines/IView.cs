// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewEngines;

/// <summary>
/// Specifies the contract for a view.
/// </summary>
public interface IView
{
    /// <summary>
    /// Gets the path of the view as resolved by the <see cref="IViewEngine"/>.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Asynchronously renders the view using the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewContext"/>.</param>
    /// <returns>A <see cref="Task"/> that on completion renders the view.</returns>
    Task RenderAsync(ViewContext context);
}
