// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for result filters, specifically <see cref="IResultFilter.OnResultExecuting"/> and
/// <see cref="IAsyncResultFilter.OnResultExecutionAsync"/> calls.
/// </summary>
public class ResultExecutingContext : FilterContext
{
    /// <summary>
    /// Instantiates a new <see cref="ResultExecutingContext"/> instance.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
    /// <param name="result">The <see cref="IActionResult"/> of the action and action filters.</param>
    /// <param name="controller">The controller instance containing the action.</param>
    public ResultExecutingContext(
        ActionContext actionContext,
        IList<IFilterMetadata> filters,
        IActionResult result,
        object controller)
        : base(actionContext, filters)
    {
        Result = result;
        Controller = controller;
    }

    /// <summary>
    /// Gets the controller instance containing the action.
    /// </summary>
    public virtual object Controller { get; }

    /// <summary>
    /// Gets or sets the <see cref="IActionResult"/> to execute. Setting <see cref="Result"/> to a non-<c>null</c>
    /// value inside a result filter will short-circuit the result and any remaining result filters.
    /// </summary>
    public virtual IActionResult Result { get; set; }

    /// <summary>
    /// Gets or sets an indication the result filter pipeline should be short-circuited.
    /// </summary>
    public virtual bool Cancel { get; set; }
}
