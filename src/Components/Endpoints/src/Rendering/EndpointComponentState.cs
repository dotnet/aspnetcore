// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

[assembly: MetadataUpdateHandler(typeof(EndpointComponentState))]

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class EndpointComponentState : ComponentState
{
    private static readonly ConcurrentDictionary<Type, StreamRenderingAttribute?> _streamRenderingAttributeByComponentType = new();

    private static readonly string _cacheBoundaryTypeName = typeof(CacheBoundary).FullName!;

    static EndpointComponentState()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += _streamRenderingAttributeByComponentType.Clear;
        }
    }

    private readonly EndpointHtmlRenderer _renderer;

    public EndpointComponentState(Renderer renderer, int componentId, IComponent component, ComponentState? parentComponentState)
        : base(renderer, componentId, component, parentComponentState)
    {
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

        if (component is CacheBoundary cacheBoundary && parentComponentState is not null)
        {
            var ancestorTypeName = parentComponentState.Component?.GetType().FullName ?? "";
            cacheBoundary.TreePositionKeyFactory = () =>
            {
                var sequence = FindSequenceInParent(parentComponentState, cacheBoundary);
                var componentKey = GetComponentKey();
                var keyString = ComponentKeyHelper.FormatSerializableKey(componentKey);
                return ComputeTreePositionKey(ancestorTypeName, sequence, keyString);
            };
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

    private static string ComputeTreePositionKey(string ancestorTypeName, int sequence, string? keyString)
    {
        return string.Concat(
            ancestorTypeName, ".",
            _cacheBoundaryTypeName, "#",
            sequence.ToString(CultureInfo.InvariantCulture),
            keyString is not null ? "." : "",
            keyString);
    }

    // We need this caclulation, because otherwise multiple CacheBoundary components under the same parent would have
    // the same key and will point to the same cache entry, which is incorrect.
    private int FindSequenceInParent(ComponentState parentState, CacheBoundary target)
    {
        var frames = _renderer.GetRenderTreeFrames(parentState.ComponentId);
        for (var i = 0; i < frames.Count; i++)
        {
            ref var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Component && ReferenceEquals(frame.Component, target))
            {
                return frame.Sequence;
            }
        }
        return 0;
    }
}
