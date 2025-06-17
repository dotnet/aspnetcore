// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Specialized ComponentState for Server/Remote rendering that supports ComponentMarkerKey for state persistence.
/// </summary>
internal sealed class RemoteComponentState : ComponentState
{
    private readonly RemoteRenderer _renderer;

    public RemoteComponentState(
        RemoteRenderer renderer,
        int componentId,
        IComponent component,
        ComponentState? parentComponentState)
        : base(renderer, componentId, component, parentComponentState)
    {
        _renderer = renderer;
    }

    protected override object? GetComponentKey()
    {
        var markerKey = _renderer.GetMarkerKey(this);

        // If we have a ComponentMarkerKey, return it for state persistence consistency
        if (markerKey != default)
        {
            return markerKey.Serialized();
        }

        // Fall back to the default implementation
        return base.GetComponentKey();
    }
}
