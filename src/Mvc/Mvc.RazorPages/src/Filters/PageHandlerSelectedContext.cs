// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for page filters, used specifically in
/// <see cref="IPageFilter.OnPageHandlerSelected(PageHandlerSelectedContext)"/> and
/// <see cref="IAsyncPageFilter.OnPageHandlerSelectionAsync(PageHandlerSelectedContext)"/>.
/// </summary>
public class PageHandlerSelectedContext : FilterContext
{
    /// <summary>
    /// Creates a new instance of <see cref="PageHandlerExecutedContext"/>.
    /// </summary>
    /// <param name="pageContext">The <see cref="PageContext"/> associated with the current request.</param>
    /// <param name="filters">The set of filters associated with the page.</param>
    /// <param name="handlerInstance">The handler instance associated with the page.</param>
    public PageHandlerSelectedContext(
        PageContext pageContext,
        IList<IFilterMetadata> filters,
        object handlerInstance)
        : base(pageContext, filters)
    {
        ArgumentNullException.ThrowIfNull(handlerInstance);

        HandlerInstance = handlerInstance;
    }

    /// <summary>
    /// Gets the descriptor associated with the current page.
    /// </summary>
    public new virtual CompiledPageActionDescriptor ActionDescriptor =>
        (CompiledPageActionDescriptor)base.ActionDescriptor;

    /// <summary>
    /// Gets or sets the descriptor for the handler method about to be invoked.
    /// </summary>
    public virtual HandlerMethodDescriptor? HandlerMethod { get; set; }

    /// <summary>
    /// Gets the object instance containing the handler method.
    /// </summary>
    public virtual object HandlerInstance { get; }
}
