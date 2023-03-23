// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// A <see cref="CircuitHandler"/> allows running code during specific lifetime events of a <see cref="Circuit"/>.
/// <list type="bullet">
/// <item><description>
/// <see cref="OnCircuitOpenedAsync(Circuit, CancellationToken)"/> is invoked after an initial circuit to the client
/// has been established.
/// </description></item>
/// <item><description>
/// <see cref="OnConnectionUpAsync(Circuit, CancellationToken)"/> is invoked immediately after the completion of
/// <see cref="OnCircuitOpenedAsync(Circuit, CancellationToken)"/>. In addition, the method is invoked each time a connection is re-established
/// with a client after it's been dropped. <see cref="OnConnectionDownAsync(Circuit, CancellationToken)"/> is invoked each time a connection
/// is dropped.
/// </description></item>
/// <item><description>
/// <see cref="OnCircuitClosedAsync(Circuit, CancellationToken)"/> is invoked prior to the server evicting the circuit to the client.
/// Application users may use this event to save state for a client that can be later rehydrated.
/// </description></item>
/// </list>
/// </summary>
public abstract class CircuitHandler
{
    /// <summary>
    /// Gets the execution order for the current instance of <see cref="CircuitHandler"/>.
    /// <para>
    /// When multiple <see cref="CircuitHandler"/> instances are registered, the <see cref="Order"/>
    /// property is used to determine the order in which instances are executed. When two handlers
    /// have the same value for <see cref="Order"/>, their execution order is non-deterministic.
    /// </para>
    /// </summary>
    /// <value>
    /// Defaults to 0.
    /// </value>
    public virtual int Order => 0;

    /// <summary>
    /// Invoked when a new circuit was established.
    /// </summary>
    /// <param name="circuit">The <see cref="Circuit"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that notifies when the client connection is aborted.</param>
    /// <returns><see cref="Task"/> that represents the asynchronous execution operation.</returns>
    public virtual Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked when a connection to the client was established.
    /// <para>
    /// This method is executed once initially after <see cref="OnCircuitOpenedAsync(Circuit, CancellationToken)"/>
    /// and once each for each reconnect during the lifetime of a circuit.
    /// </para>
    /// </summary>
    /// <param name="circuit">The <see cref="Circuit"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that notifies when the client connection is aborted.</param>
    /// <returns><see cref="Task"/> that represents the asynchronous execution operation.</returns>
    public virtual Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked when a connection to the client was dropped.
    /// </summary>
    /// <param name="circuit">The <see cref="Circuit"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> that represents the asynchronous execution operation.</returns>
    public virtual Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked when a new circuit is being discarded.
    /// </summary>
    /// <param name="circuit">The <see cref="Circuit"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> that represents the asynchronous execution operation.</returns>
    public virtual Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Creates a handler that gets invoked when inbound activity on the circuit causes an asynchronous task to be dispatched on the server.
    /// </summary>
    /// <param name="next">The next handler to invoke.</param>
    /// <returns>A handler function that returns a <see cref="Task"/> that completes when the activity has finished.</returns>
    public virtual Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) => next;
}
