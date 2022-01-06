// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Interface that exposes the ability to create an <see cref="IViewComponentInvoker"/>.
/// </summary>
public interface IViewComponentInvokerFactory
{
    /// <summary>
    /// Creates a <see cref="IViewComponentInvoker"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
    /// <returns>The <see cref="IViewComponentInvoker"/>.</returns>
    IViewComponentInvoker CreateInstance(ViewComponentContext context);
}
