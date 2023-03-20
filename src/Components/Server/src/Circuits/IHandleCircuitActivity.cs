// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// A handler to process inbound circuit activity.
/// </summary>
public interface IHandleCircuitActivity
{
    /// <summary>
    /// Invoked when inbound activity on the circuit causes an asynchronous task to be dispatched on the server.
    /// </summary>
    /// <param name="context">The <see cref="CircuitInboundActivityContext"/>.</param>
    /// <param name="next">The next handler to invoke.</param>
    /// <returns>A <see cref="Task"/> that completes when the activity has finished.</returns>
    Task HandleInboundActivityAsync(CircuitInboundActivityContext context, Func<CircuitInboundActivityContext, Task> next);
}
