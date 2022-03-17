// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that asynchronously surrounds execution of action results successfully returned from an action.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IResultFilter"/> and <see cref="IAsyncResultFilter"/> implementations are executed around the action
/// result only when the action method (or action filters) complete successfully.
/// </para>
/// <para>
/// <see cref="IResultFilter"/> and <see cref="IAsyncResultFilter"/> instances are not executed in cases where
/// an authorization filter or resource filter short-circuits the request to prevent execution of the action.
/// <see cref="IResultFilter"/>. <see cref="IResultFilter"/> and <see cref="IAsyncResultFilter"/> implementations
/// are also not executed in cases where an exception filter handles an exception by producing an action result.
/// </para>
/// <para>
/// To create a result filter that surrounds the execution of all action results, implement
/// either the <see cref="IAlwaysRunResultFilter"/> or the <see cref="IAsyncAlwaysRunResultFilter"/> interface.
/// </para>
/// </remarks>
public interface IAsyncResultFilter : IFilterMetadata
{
    /// <summary>
    /// Called asynchronously before the action result.
    /// </summary>
    /// <param name="context">The <see cref="ResultExecutingContext"/>.</param>
    /// <param name="next">
    /// The <see cref="ResultExecutionDelegate"/>. Invoked to execute the next result filter or the result itself.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
    Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
}
