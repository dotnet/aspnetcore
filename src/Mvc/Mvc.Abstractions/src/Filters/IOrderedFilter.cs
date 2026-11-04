// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that specifies the relative order it should run.
/// </summary>
public interface IOrderedFilter : IFilterMetadata
{
    /// <summary>
    /// Gets the order value for determining the order of execution of filters. Filters execute in
    /// ascending numeric value of the <see cref="Order"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filters are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
    /// </para>
    /// <para>
    /// Asynchronous filters, such as <see cref="IAsyncActionFilter"/>, surround the execution of subsequent
    /// filters of the same filter kind. An asynchronous filter with a lower numeric <see cref="Order"/>
    /// value will have its filter method, such as <see cref="IAsyncActionFilter.OnActionExecutionAsync"/>,
    /// executed before that of a filter with a higher value of <see cref="Order"/>.
    /// </para>
    /// <para>
    /// Synchronous filters, such as <see cref="IActionFilter"/>, have a before-method, such as
    /// <see cref="IActionFilter.OnActionExecuting"/>, and an after-method, such as
    /// <see cref="IActionFilter.OnActionExecuted"/>. A synchronous filter with a lower numeric <see cref="Order"/>
    /// value will have its before-method executed before that of a filter with a higher value of
    /// <see cref="Order"/>. During the after-stage of the filter, a synchronous filter with a lower
    /// numeric <see cref="Order"/> value will have its after-method executed after that of a filter with a higher
    /// value of <see cref="Order"/>.
    /// </para>
    /// <para>
    /// If two filters have the same numeric value of <see cref="Order"/>, then their relative execution order
    /// is determined by the filter scope.
    /// </para>
    /// </remarks>
    int Order { get; }
}
