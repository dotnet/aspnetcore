// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Supports the rendering of view components in a view.
/// </summary>
public interface IViewComponentHelper
{
    /// <summary>
    /// Invokes a view component with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the view component.</param>
    /// <param name="arguments">
    /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
    /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
    /// containing the invocation arguments.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion returns the rendered <see cref="IHtmlContent" />.
    /// </returns>
    Task<IHtmlContent> InvokeAsync(string name, object? arguments);

    /// <summary>
    /// Invokes a view component of type <paramref name="componentType" />.
    /// </summary>
    /// <param name="componentType">The view component <see cref="Type"/>.</param>
    /// <param name="arguments">
    /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
    /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
    /// containing the invocation arguments.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion returns the rendered <see cref="IHtmlContent" />.
    /// </returns>
    Task<IHtmlContent> InvokeAsync(Type componentType, object? arguments);
}
