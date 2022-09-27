// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for action filters, specifically <see cref="IActionFilter.OnActionExecuted"/> calls.
/// </summary>
public class ActionExecutedContext : FilterContext
{
    private Exception? _exception;
    private ExceptionDispatchInfo? _exceptionDispatchInfo;

    /// <summary>
    /// Instantiates a new <see cref="ActionExecutingContext"/> instance.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
    /// <param name="controller">The controller instance containing the action.</param>
    public ActionExecutedContext(
        ActionContext actionContext,
        IList<IFilterMetadata> filters,
        object controller)
        : base(actionContext, filters)
    {
        Controller = controller;
    }

    /// <summary>
    /// Gets or sets an indication that an action filter short-circuited the action and the action filter pipeline.
    /// </summary>
    public virtual bool Canceled { get; set; }

    /// <summary>
    /// Gets the controller instance containing the action.
    /// </summary>
    public virtual object Controller { get; }

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
    /// Gets or sets the <see cref="IActionResult"/>.
    /// </summary>
    public virtual IActionResult? Result { get; set; }
}
