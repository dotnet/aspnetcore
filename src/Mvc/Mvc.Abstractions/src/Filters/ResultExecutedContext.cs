// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for result filters, specifically <see cref="IResultFilter.OnResultExecuted"/> calls.
/// </summary>
public class ResultExecutedContext : FilterContext
{
    private Exception? _exception;
    private ExceptionDispatchInfo? _exceptionDispatchInfo;

    /// <summary>
    /// Instantiates a new <see cref="ResultExecutedContext"/> instance.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
    /// <param name="result">
    /// The <see cref="IActionResult"/> copied from <see cref="ResultExecutingContext.Result"/>.
    /// </param>
    /// <param name="controller">The controller instance containing the action.</param>
    public ResultExecutedContext(
        ActionContext actionContext,
        IList<IFilterMetadata> filters,
        IActionResult result,
        object controller)
        : base(actionContext, filters)
    {
        ArgumentNullException.ThrowIfNull(result);

        Result = result;
        Controller = controller;
    }

    /// <summary>
    /// Gets or sets an indication that a result filter set <see cref="ResultExecutingContext.Cancel"/> to
    /// <c>true</c> and short-circuited the filter pipeline.
    /// </summary>
    public virtual bool Canceled { get; set; }

    /// <summary>
    /// Gets the controller instance containing the action.
    /// </summary>
    public virtual object Controller { get; }

    /// <summary>
    /// Gets or sets the <see cref="System.Exception"/> caught while executing the result or result filters, if
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
        get
        {
            return _exceptionDispatchInfo;
        }

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
    /// Gets the <see cref="IActionResult"/> copied from <see cref="ResultExecutingContext.Result"/>.
    /// </summary>
    public virtual IActionResult Result { get; }
}
