// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// The filter abstraction for hub method invocations.
/// </summary>
public interface IHubFilter
{
    /// <summary>
    /// Allows handling of all Hub method invocations.
    /// </summary>
    /// <param name="invocationContext">The context for the method invocation that holds all the important information about the invoke.</param>
    /// <param name="next">The next filter to run, and for the final one, the Hub invocation.</param>
    /// <returns>Returns the result of the Hub method invoke.</returns>
    ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next) => next(invocationContext);

    /// <summary>
    /// Allows handling of the <see cref="Hub.OnConnectedAsync"/> method.
    /// </summary>
    /// <param name="context">The context for OnConnectedAsync.</param>
    /// <param name="next">The next filter to run, and for the final one, the Hub invocation.</param>
    /// <returns></returns>
    Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next) => next(context);

    /// <summary>
    /// Allows handling of the <see cref="Hub.OnDisconnectedAsync(Exception)"/> method.
    /// </summary>
    /// <param name="context">The context for OnDisconnectedAsync.</param>
    /// <param name="exception">The exception, if any, for the connection closing.</param>
    /// <param name="next">The next filter to run, and for the final one, the Hub invocation.</param>
    /// <returns></returns>
    Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next) => next(context, exception);
}
