// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// An abstract filter that runs asynchronously after an action has thrown an <see cref="Exception"/>. Subclasses
/// must override <see cref="OnException"/> or <see cref="OnExceptionAsync"/> but not both.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class ExceptionFilterAttribute : Attribute, IAsyncExceptionFilter, IExceptionFilter, IOrderedFilter
{
    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public virtual Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        OnException(context);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void OnException(ExceptionContext context)
    {
    }
}
