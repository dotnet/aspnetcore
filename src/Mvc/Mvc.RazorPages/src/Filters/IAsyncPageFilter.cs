// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that asynchronously surrounds execution of a page handler method. This filter is executed only when
/// decorated on a handler's type and not on individual handler methods.
/// </summary>
public interface IAsyncPageFilter : IFilterMetadata
{
    /// <summary>
    /// Called asynchronously after the handler method has been selected, but before model binding occurs.
    /// </summary>
    /// <param name="context">The <see cref="PageHandlerSelectedContext"/>.</param>
    /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
    Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context);

    /// <summary>
    /// Called asynchronously before the handler method is invoked, after model binding is complete.
    /// </summary>
    /// <param name="context">The <see cref="PageHandlerExecutingContext"/>.</param>
    /// <param name="next">
    /// The <see cref="PageHandlerExecutionDelegate"/>. Invoked to execute the next page filter or the handler method itself.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
    Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next);
}
