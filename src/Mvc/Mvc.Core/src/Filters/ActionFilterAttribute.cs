// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// An abstract filter that asynchronously surrounds execution of the action and the action result. Subclasses
/// should override <see cref="OnActionExecuting"/>, <see cref="OnActionExecuted"/> or
/// <see cref="OnActionExecutionAsync"/> but not <see cref="OnActionExecutionAsync"/> and either of the other two.
/// Similarly subclasses should override <see cref="OnResultExecuting"/>, <see cref="OnResultExecuted"/> or
/// <see cref="OnResultExecutionAsync"/> but not <see cref="OnResultExecutionAsync"/> and either of the other two.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class ActionFilterAttribute :
    Attribute, IActionFilter, IAsyncActionFilter, IResultFilter, IAsyncResultFilter, IOrderedFilter
{
    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public virtual void OnActionExecuting(ActionExecutingContext context)
    {
    }

    /// <inheritdoc />
    public virtual void OnActionExecuted(ActionExecutedContext context)
    {
    }

    /// <inheritdoc />
    public virtual async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        OnActionExecuting(context);
        if (context.Result == null)
        {
            OnActionExecuted(await next());
        }
    }

    /// <inheritdoc />
    public virtual void OnResultExecuting(ResultExecutingContext context)
    {
    }

    /// <inheritdoc />
    public virtual void OnResultExecuted(ResultExecutedContext context)
    {
    }

    /// <inheritdoc />
    public virtual async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        OnResultExecuting(context);
        if (!context.Cancel)
        {
            OnResultExecuted(await next());
        }
    }
}
