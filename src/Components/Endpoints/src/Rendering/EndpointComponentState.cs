// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

[assembly: MetadataUpdateHandler(typeof(EndpointComponentState))]

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class EndpointComponentState : ComponentState
{
    private static readonly ConcurrentDictionary<Type, StreamRenderingAttribute?> _streamRenderingAttributeByComponentType = new();
    private static readonly ConcurrentDictionary<(string, string?), string> _treePositionKeyCache = new();
    private static readonly string _cacheBoundaryTypeName = typeof(CacheBoundary).FullName!;

    static EndpointComponentState()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += _streamRenderingAttributeByComponentType.Clear;
            HotReloadManager.Default.OnDeltaApplied += _treePositionKeyCache.Clear;
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

        if (component is CacheBoundary cacheBoundary)
        {
            var ancestorTypeName = parentComponentState?.Component?.GetType().FullName ?? "";
            cacheBoundary.TreePositionKeyFactory = () =>
            {
                var (componentKey, sequence) = GetComponentKeyAndSequence();
                return ComputeTreePositionKey(ancestorTypeName, componentKey, sequence);
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

    private (object? Key, int Sequence) GetComponentKeyAndSequence()
    {
        if (ParentComponentState is not { } parentState)
        {
            return (null, 0);
        }

        var frames = _renderer.GetRenderTreeFrames(parentState.ComponentId);
        for (var i = 0; i < frames.Count; i++)
        {
            ref var currentFrame = ref frames.Array[i];
            if (currentFrame.FrameType == RenderTreeFrameType.Component &&
                ReferenceEquals(Component, currentFrame.Component))
            {
                return (currentFrame.ComponentKey, currentFrame.Sequence);
            }
        }

        return (null, 0);
    }

    /// <summary>
    /// MetadataUpdateHandler event. This is invoked by the hot reload host via reflection.
    /// </summary>
    public static void UpdateApplication(Type[]? _)
    {
        _streamRenderingAttributeByComponentType.Clear();
        _treePositionKeyCache.Clear();
    }

    private static string ComputeTreePositionKey(string ancestorTypeName, object? componentKey, int sequence)
    {
        var keyString = FormatSerializableKey(componentKey);
        var seqString = sequence.ToString(CultureInfo.InvariantCulture);

        if (keyString is null)
        {
            return _treePositionKeyCache.GetOrAdd((ancestorTypeName, seqString), static parts =>
                Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(
                    string.Concat(parts.Item1, ".", _cacheBoundaryTypeName, ".", parts.Item2)))));
        }

        return _treePositionKeyCache.GetOrAdd((ancestorTypeName, string.Concat(seqString, ".", keyString)), static parts =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(
                string.Concat(parts.Item1, ".", _cacheBoundaryTypeName, ".", parts.Item2)))));
    }

    private static string? FormatSerializableKey(object? key)
    {
        if (key is null)
        {
            return null;
        }

        var keyType = key.GetType();
        var isSerializable = Type.GetTypeCode(keyType) != TypeCode.Object
            || keyType == typeof(Guid)
            || keyType == typeof(DateTimeOffset)
            || keyType == typeof(DateOnly)
            || keyType == typeof(TimeOnly);

        if (!isSerializable)
        {
            return null;
        }

        return key switch
        {
            IFormattable formattable => formattable.ToString("", CultureInfo.InvariantCulture),
            IConvertible convertible => convertible.ToString(CultureInfo.InvariantCulture),
            _ => key.ToString(),
        };
    }
}
