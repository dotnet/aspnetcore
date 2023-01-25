// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for page filters, used specifically in
/// <see cref="IPageFilter.OnPageHandlerExecuting(PageHandlerExecutingContext)"/> and
/// <see cref="IAsyncPageFilter.OnPageHandlerExecutionAsync(PageHandlerExecutingContext, PageHandlerExecutionDelegate)"/>.
/// </summary>
public class PageHandlerExecutingContext : FilterContext
{
    /// <summary>
    /// Creates a new instance of <see cref="PageHandlerExecutingContext"/>.
    /// </summary>
    /// <param name="pageContext">The <see cref="PageContext"/> associated with the current request.</param>
    /// <param name="filters">The set of filters associated with the page.</param>
    /// <param name="handlerMethod">The handler method to be invoked, may be null.</param>
    /// <param name="handlerArguments">The arguments to provide to the handler method.</param>
    /// <param name="handlerInstance">The handler instance associated with the page.</param>
    public PageHandlerExecutingContext(
        PageContext pageContext,
        IList<IFilterMetadata> filters,
        HandlerMethodDescriptor? handlerMethod,
        IDictionary<string, object?> handlerArguments,
        object handlerInstance)
        : base(pageContext, filters)
    {
        ArgumentNullException.ThrowIfNull(handlerArguments);
        ArgumentNullException.ThrowIfNull(handlerInstance);

        HandlerMethod = handlerMethod;
        HandlerArguments = handlerArguments;
        HandlerInstance = handlerInstance;
    }

    /// <summary>
    /// Gets the descriptor associated with the current page.
    /// </summary>
    public new virtual CompiledPageActionDescriptor ActionDescriptor =>
        (CompiledPageActionDescriptor)base.ActionDescriptor;

    /// <summary>
    /// Gets or sets the <see cref="IActionResult"/> to execute. Setting <see cref="Result"/> to a non-<c>null</c>
    /// value inside a page filter will short-circuit the page and any remaining page filters.
    /// </summary>
    public virtual IActionResult? Result { get; set; }

    /// <summary>
    /// Gets the arguments to pass when invoking the handler method. Keys are parameter names.
    /// </summary>
    public virtual IDictionary<string, object?> HandlerArguments { get; }

    /// <summary>
    /// Gets the descriptor for the handler method about to be invoked.
    /// </summary>
    public virtual HandlerMethodDescriptor? HandlerMethod { get; }

    /// <summary>
    /// Gets the object instance containing the handler method.
    /// </summary>
    public virtual object HandlerInstance { get; }
}
