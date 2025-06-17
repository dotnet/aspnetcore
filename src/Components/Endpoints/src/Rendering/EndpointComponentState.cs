// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

[assembly: MetadataUpdateHandler(typeof(EndpointComponentState))]

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class EndpointComponentState : ComponentState
{
    private static readonly ConcurrentDictionary<Type, StreamRenderingAttribute?> _streamRenderingAttributeByComponentType = new();

    public EndpointComponentState(Renderer renderer, int componentId, IComponent component, ComponentState? parentComponentState)
        : base(renderer, componentId, component, parentComponentState)
    {
        var streamRenderingAttribute = _streamRenderingAttributeByComponentType.GetOrAdd(component.GetType(),
            type => type.GetCustomAttribute<StreamRenderingAttribute>());

        if (streamRenderingAttribute is not null)
        {
            StreamRendering = streamRenderingAttribute.Enabled;
        }
        else
        {
            var parentEndpointComponentState = (EndpointComponentState?)LogicalParentComponentState;
            StreamRendering = parentEndpointComponentState?.StreamRendering ?? false;
        }
    }

    public bool StreamRendering { get; }

    protected override object? GetComponentKey()
    {
        // Check if the parent Component is an SSRRenderModeBoundary instance.
        // We do this by checking if the grandparent (.Parent.Parent) doesn't have a render mode,
        // which means that .Parent is an SSRRenderModeBoundary component.
        if (ParentComponentState?.ParentComponentState is { } grandParent)
        {
            var grandParentRenderMode = Renderer.GetComponentRenderMode(grandParent.Component);
            if (grandParentRenderMode is null)
            {
                // The parent is likely an SSRRenderModeBoundary - we need to get the ComponentMarkerKey from it
                if (ParentComponentState.Component is SSRRenderModeBoundary boundary)
                {
                    return GetComponentMarkerKeyFromBoundary(boundary);
                }
            }
        }

        // Fall back to the default implementation
        return base.GetComponentKey();
    }

    private static object? GetComponentMarkerKeyFromBoundary(SSRRenderModeBoundary boundary)
    {
        // Get the ComponentMarkerKey from the SSRRenderModeBoundary
        return boundary.GetComponentMarkerKey();
    }

    /// <summary>
    /// MetadataUpdateHandler event. This is invoked by the hot reload host via reflection.
    /// </summary>
    public static void UpdateApplication(Type[]? _) => _streamRenderingAttributeByComponentType.Clear();
}
