// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed class CircuitHandleRegistry : ICircuitHandleRegistry
{
    public CircuitHandle GetCircuitHandle(IDictionary<object, object?> circuitHandles, object circuitKey)
    {
        if (circuitHandles.TryGetValue(circuitKey, out var circuitHandle))
        {
            return (CircuitHandle)circuitHandle;
        }

        return null;
    }

    public CircuitHost GetCircuit(IDictionary<object, object?> circuitHandles, object circuitKey)
    {
        if (circuitHandles.TryGetValue(circuitKey, out var circuitHandle))
        {
            return ((CircuitHandle)circuitHandle).CircuitHost;
        }

        return null;
    }

    public void SetCircuit(IDictionary<object, object?> circuitHandles, object circuitKey, CircuitHost circuitHost)
    {
        circuitHandles[circuitKey] = circuitHost?.Handle;
    }
}
