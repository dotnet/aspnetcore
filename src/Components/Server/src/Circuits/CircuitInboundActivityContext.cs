// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Contains information about inbound <see cref="Circuits.Circuit"/> activity.
/// </summary>
public sealed class CircuitInboundActivityContext
{
    internal Func<Task> Handler { get; }

    /// <summary>
    /// Gets the <see cref="Circuits.Circuit"/> associated with the activity.
    /// </summary>
    public Circuit Circuit { get; }

    internal CircuitInboundActivityContext(Func<Task> handler, Circuit circuit)
    {
        Handler = handler;
        Circuit = circuit;
    }
}
