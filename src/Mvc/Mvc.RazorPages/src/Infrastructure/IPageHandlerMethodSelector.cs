// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// Selects a handler method from a page.
/// </summary>
public interface IPageHandlerMethodSelector
{
    /// <summary>
    /// Selects a handler method from a page.
    /// </summary>
    /// <param name="context">The <see cref="PageContext"/>.</param>
    /// <returns>The selected <see cref="HandlerMethodDescriptor"/>.</returns>
    HandlerMethodDescriptor? Select(PageContext context);
}
