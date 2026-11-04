// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for page filters, used specifically in
/// <see cref="IPageFilter.OnPageHandlerExecuted(PageHandlerExecutedContext)"/> and
/// <see cref="IAsyncPageFilter.OnPageHandlerExecutionAsync(PageHandlerExecutingContext, PageHandlerExecutionDelegate)"/>.
/// </summary>
public class PageHandlerExecutedContext : FilterContext
{
    private Exception? _exception;
    private ExceptionDispatchInfo? _exceptionDispatchInfo;

    /// <summary>
    /// Creates a new instance of <see cref="PageHandlerExecutedContext"/>.
    /// </summary>
    /// <param name="pageContext">The <see cref="PageContext"/> associated with the current request.</param>
    /// <param name="filters">The set of filters associated with the page.</param>
    /// <param name="handlerMethod">The handler method to be invoked, may be null.</param>
    /// <param name="handlerInstance">The handler instance associated with the page.</param>
    public PageHandlerExecutedContext(
        PageContext pageContext,
        IList<IFilterMetadata> filters,
        HandlerMethodDescriptor? handlerMethod,
        object handlerInstance)
        : base(pageContext, filters)
    {
        ArgumentNullException.ThrowIfNull(handlerInstance);

        HandlerMethod = handlerMethod;
        HandlerInstance = handlerInstance;
    }

    /// <summary>
    /// Gets the descriptor associated with the current page.
    /// </summary>
    public new virtual CompiledPageActionDescriptor ActionDescriptor =>
        (CompiledPageActionDescriptor)base.ActionDescriptor;

    /// <summary>
    /// Gets or sets an indication that an page filter short-circuited the action and the page filter pipeline.
    /// </summary>
    public virtual bool Canceled { get; set; }

    /// <summary>
    /// Gets the handler instance containing the handler method.
    /// </summary>
    public virtual object HandlerInstance { get; }

    /// <summary>
    /// Gets the descriptor for the handler method that was invoked.
    /// </summary>
    public virtual HandlerMethodDescriptor? HandlerMethod { get; }

    /// <summary>
    /// Gets or sets the <see cref="System.Exception"/> caught while executing the action or action filters, if
    /// any.
    /// </summary>
    public virtual Exception? Exception
    {
        get
        {
            if (_exception == null && _exceptionDispatchInfo != null)
            {
                return _exceptionDispatchInfo.SourceException;
            }
            else
            {
                return _exception;
            }
        }

        set
        {
            _exceptionDispatchInfo = null;
            _exception = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="System.Runtime.ExceptionServices.ExceptionDispatchInfo"/> for the
    /// <see cref="Exception"/>, if an <see cref="System.Exception"/> was caught and this information captured.
    /// </summary>
    public virtual ExceptionDispatchInfo? ExceptionDispatchInfo
    {
        get => _exceptionDispatchInfo;

        set
        {
            _exception = null;
            _exceptionDispatchInfo = value;
        }
    }

    /// <summary>
    /// Gets or sets an indication that the <see cref="Exception"/> has been handled.
    /// </summary>
    public virtual bool ExceptionHandled { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IActionResult"/>.
    /// </summary>
    public virtual IActionResult? Result { get; set; }
}
