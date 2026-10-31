// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Result type of a <see cref="ViewComponent"/>.
/// </summary>
public interface IViewComponentResult
{
    /// <summary>
    /// Executes the result of a <see cref="ViewComponent"/> using the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
    void Execute(ViewComponentContext context);

    /// <summary>
    /// Asynchronously executes the result of a <see cref="ViewComponent"/> using the specified
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous execution.</returns>
    Task ExecuteAsync(ViewComponentContext context);
}
