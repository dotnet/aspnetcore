// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CircuitEnabledBoundaryRegistry
{
    private readonly HashSet<SSRRenderModeBoundary> _circuitEnabledBoundaries = new();

    public int CircuitEnabledBoundaryCount => _circuitEnabledBoundaries.Count;

    public void RegisterCircuitEnabledBoundary(SSRRenderModeBoundary boundary)
    {
        _circuitEnabledBoundaries.Add(boundary);
    }

    public void UnregisterCircuitEnabledBoundary(SSRRenderModeBoundary boundary)
    {
        _circuitEnabledBoundaries.Remove(boundary);
    }
}
