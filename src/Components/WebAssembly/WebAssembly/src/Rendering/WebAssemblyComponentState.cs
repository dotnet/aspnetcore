// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

/// <summary>
/// Specialized ComponentState for WebAssembly rendering that supports ComponentMarkerKey for state persistence.
/// </summary>
internal sealed class WebAssemblyComponentState : ComponentState
{
    private readonly ComponentMarkerKey? _componentMarkerKey;

    public WebAssemblyComponentState(
        Renderer renderer, 
        int componentId, 
        IComponent component, 
        ComponentState? parentComponentState,
        ComponentMarkerKey? componentMarkerKey = null)
        : base(renderer, componentId, component, parentComponentState)
    {
        _componentMarkerKey = componentMarkerKey;
    }

    protected override object? GetComponentKey()
    {
        // If we have a ComponentMarkerKey, return it for state persistence consistency
        if (_componentMarkerKey.HasValue)
        {
            return _componentMarkerKey.Value;
        }

        // Fall back to the default implementation
        return base.GetComponentKey();
    }
}