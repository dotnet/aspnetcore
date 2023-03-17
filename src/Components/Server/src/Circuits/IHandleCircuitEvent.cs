// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// A handler to process inbound circuit events.
/// </summary>
public interface IHandleCircuitEvent
{
    /// <summary>
    /// Invoked when inbound event on the circuit causes an asynchronous task to be dispatched on the server.
    /// </summary>
    /// <param name="context">The <see cref="CircuitInboundEventContext"/>.</param>
    /// <param name="next">The next handler to invoke.</param>
    /// <returns>A <see cref="Task"/> that completes when the event has finished.</returns>
    Task HandleInboundEventAsync(CircuitInboundEventContext context, Func<CircuitInboundEventContext, Task> next);
}
