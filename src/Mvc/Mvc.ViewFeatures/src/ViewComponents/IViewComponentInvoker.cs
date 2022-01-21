// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Specifies the contract for execution of a view component.
/// </summary>
public interface IViewComponentInvoker
{
    /// <summary>
    /// Executes the view component specified by <see cref="ViewComponentContext.ViewComponentDescriptor"/>
    /// of <paramref name="context"/> and writes the result to <see cref="ViewComponentContext.Writer"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation of execution.</returns>
    Task InvokeAsync(ViewComponentContext context);
}
