// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
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
    private readonly EndpointHtmlRenderer _renderer;
    public EndpointComponentState(Renderer renderer, int componentId, IComponent component, ComponentState? parentComponentState)
        : base(renderer, componentId, component, parentComponentState)
    {
        Debug.Assert(renderer != null && renderer is EndpointHtmlRenderer);
        _renderer = (EndpointHtmlRenderer)renderer;

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
        if (ParentComponentState != null && ParentComponentState.Component is SSRRenderModeBoundary boundary)
        {
            var (sequence, key) = _renderer.GetSequenceAndKey(ParentComponentState);
            var marker = boundary.GetComponentMarkerKey(sequence, key);
            if (!marker.Equals(default))
            {
                return marker.Serialized();
            }
        }

        // Fall back to the default implementation
        return base.GetComponentKey();
    }

    /// <summary>
    /// MetadataUpdateHandler event. This is invoked by the hot reload host via reflection.
    /// </summary>
    public static void UpdateApplication(Type[]? _) => _streamRenderingAttributeByComponentType.Clear();
}
