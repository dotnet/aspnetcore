// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that surrounds execution of a page handler method. This filter is executed only when decorated on a
/// handler's type and not on individual handler methods.
/// </summary>
public interface IPageFilter : IFilterMetadata
{
    /// <summary>
    /// Called after a handler method has been selected, but before model binding occurs.
    /// </summary>
    /// <param name="context">The <see cref="PageHandlerSelectedContext"/>.</param>
    void OnPageHandlerSelected(PageHandlerSelectedContext context);

    /// <summary>
    /// Called before the handler method executes, after model binding is complete.
    /// </summary>
    /// <param name="context">The <see cref="PageHandlerExecutingContext"/>.</param>
    void OnPageHandlerExecuting(PageHandlerExecutingContext context);

    /// <summary>
    /// Called after the handler method executes, before the action result executes.
    /// </summary>
    /// <param name="context">The <see cref="PageHandlerExecutedContext"/>.</param>
    void OnPageHandlerExecuted(PageHandlerExecutedContext context);
}
