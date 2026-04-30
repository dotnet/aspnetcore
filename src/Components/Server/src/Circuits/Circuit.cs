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
    /// Requests that the connected client begin the graceful circuit-pause flow.
    /// </summary>
    /// <remarks>
    /// The operation is idempotent. Observe completion through
    /// <see cref="CircuitHandler.OnConnectionDownAsync"/> and <see cref="CircuitHandler.OnCircuitClosedAsync"/>.
    /// </remarks>
    /// <param name="cancellationToken">
    /// Cancels the request before it is accepted by the framework.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the request was accepted and the client was asked to begin pausing;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public ValueTask<bool> RequestCircuitPauseAsync(CancellationToken cancellationToken = default)
    {
        return _circuitHost.RequestPauseAsync(cancellationToken);
    }
}
