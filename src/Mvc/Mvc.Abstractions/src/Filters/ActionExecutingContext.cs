// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for action filters, specifically <see cref="IActionFilter.OnActionExecuting"/> and
/// <see cref="IAsyncActionFilter.OnActionExecutionAsync"/> calls.
/// </summary>
public class ActionExecutingContext : FilterContext
{
    /// <summary>
    /// Instantiates a new <see cref="ActionExecutingContext"/> instance.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
    /// <param name="actionArguments">
    /// The arguments to pass when invoking the action. Keys are parameter names.
    /// </param>
    /// <param name="controller">The controller instance containing the action.</param>
    public ActionExecutingContext(
        ActionContext actionContext,
        IList<IFilterMetadata> filters,
        IDictionary<string, object?> actionArguments,
        object controller)
        : base(actionContext, filters)
    {
        ArgumentNullException.ThrowIfNull(actionArguments);

        ActionArguments = actionArguments;
        Controller = controller;
    }

    /// <summary>
    /// Gets or sets the <see cref="IActionResult"/> to execute. Setting <see cref="Result"/> to a non-<c>null</c>
    /// value inside an action filter will short-circuit the action and any remaining action filters.
    /// </summary>
    public virtual IActionResult? Result { get; set; }

    /// <summary>
    /// Gets the arguments to pass when invoking the action. Keys are parameter names.
    /// </summary>
    public virtual IDictionary<string, object?> ActionArguments { get; }

    /// <summary>
    /// Gets the controller instance containing the action.
    /// </summary>
    public virtual object Controller { get; }
}
