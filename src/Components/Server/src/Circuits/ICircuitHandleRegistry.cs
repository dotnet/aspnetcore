// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal interface ICircuitHandleRegistry
{
    CircuitHandle GetCircuitHandle(IDictionary<object, object?> circuitHandles, object circuitKey);

    CircuitHost GetCircuit(IDictionary<object, object?> circuitHandles, object circuitKey);

    void SetCircuit(IDictionary<object, object?> circuitHandles, object circuitKey, CircuitHost circuitHost);
}
