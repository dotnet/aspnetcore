// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Contains information about an inbound <see cref="Circuits.Circuit"/> event.
/// </summary>
public sealed class CircuitInboundEventContext
{
    internal Func<Task> Handler { get; }

    /// <summary>
    /// Gets the <see cref="Circuits.Circuit"/> associated with the event.
    /// </summary>
    public Circuit Circuit { get; }

    internal CircuitInboundEventContext(Func<Task> handler, Circuit circuit)
    {
        Handler = handler;
        Circuit = circuit;
    }
}
