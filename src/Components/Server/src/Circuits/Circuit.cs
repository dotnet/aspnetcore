// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Represents a link between a ASP.NET Core Component on the server and a client.
/// </summary>
public sealed class Circuit
{
    private readonly CircuitHost _circuitHost;

    internal Circuit(CircuitHost circuitHost)
    {
        _circuitHost = circuitHost;
    }

    /// <summary>
    /// Gets the identifier for the <see cref="Circuit"/>.
    /// </summary>
    public string Id => _circuitHost.CircuitId.Id;

    /// <summary>
    /// Notifies the client that the circuit must be paused and lets
    /// the client start the pause process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Graceful pauses always start client-side. This method only notifies
    /// the client that it should start the pause process, but it does not
    /// start the actual pause operation.
    /// </para>
    /// <para>
    /// If the notification fails due to a disconnection, the circuit continues
    /// operating normally, transitioning to a disconnected state and eventually
    /// performing an ungraceful pause if the client does not reconnect.
    /// </para>
    /// </remarks>
    public void PauseCircuit()
    {
        _circuitHost.TriggerCircuitPause();
    }
}
