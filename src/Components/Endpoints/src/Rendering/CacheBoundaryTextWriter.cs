// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CacheBoundaryTextWriter : TextWriter
{
    // Approximate per-live-cached-component footprint used only for cache size accounting; live cached components
    // are component nodes rather than strings, so they have no captured length to measure directly.
    private const int LiveCachedComponentSizeEstimate = 256;

    private readonly TextWriter _innerWriter;
    private readonly StringBuilder _buffer = new();
    private readonly List<CacheCaptureEntry> _entries = [];
    private bool _capturing;
    private bool _validateOnly;

    public CacheBoundaryTextWriter(TextWriter inner, CacheBoundaryVaryBy varyBy)
    {
        _innerWriter = inner;
        VaryBy = varyBy;
    }

    public CacheBoundaryVaryBy VaryBy { get; set; }

    public bool IsCapturing => _capturing;

    public bool IsValidationOnly => _validateOnly;

    public override Encoding Encoding => _innerWriter.Encoding;

    public override void Write(char value)
    {
        _innerWriter.Write(value);
        if (_capturing && !_validateOnly)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        _innerWriter.Write(value);
        if (_capturing && !_validateOnly)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _innerWriter.Write(buffer, index, count);
        if (_capturing && !_validateOnly)
        {
            _buffer.Append(buffer, index, count);
        }
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        _innerWriter.Write(buffer);
        if (_capturing && !_validateOnly)
        {
            _buffer.Append(buffer);
        }
    }

    public void PauseCapture()
    {
        FlushBuffer();
        _capturing = false;
    }

    public void StartCapture()
    {
        _capturing = true;
    }

    public void StartValidation()
    {
        _capturing = true;
        _validateOnly = true;
    }

    public void CreateLiveCachedComponent(Type componentType, IComponentRenderMode? renderMode, RenderFragmentCapture capture, ILogger renderFragmentSerializationLogger)
    {
        ThrowIfLiveCachedComponentHasRenderFragmentParameter(componentType, capture);

        RenderTreeNode? liveCachedComponentNode = null;
        foreach (var node in RenderFragmentSerializer.SerializeFrames(capture, renderFragmentSerializationLogger))
        {
            if (node.Type is "component")
            {
                liveCachedComponentNode = node;
                break;
            }
        }

        if (liveCachedComponentNode is null)
        {
            throw new InvalidOperationException(
                $"CacheBoundary could not serialize the live cached component '{componentType.FullName}' from its parent's render tree.");
        }

        // The serializer fills RenderModeName from an inline @rendermode frame. For components that
        // declare their render mode via [RenderModeAttribute] instead, the capture has no render-mode
        // frame, so patch it from the boundary's runtime render mode.
        var renderModeName = RenderFragmentSerializer.GetRenderModeName(renderMode);
        if (renderModeName is not null && liveCachedComponentNode.RenderModeName is null)
        {
            liveCachedComponentNode.RenderModeName = renderModeName;
            liveCachedComponentNode.Prerender = RenderFragmentSerializer.GetRenderModePrerender(renderMode);
        }

        _entries.Add(CacheCaptureEntry.LiveCachedComponent(liveCachedComponentNode));
    }

    public void StopCapture()
    {
        _capturing = false;
        FlushBuffer();
    }

    // Assembles the cache payload by walking the recorded entries in render order: markup segments become
    // markup nodes and live cached components contribute the component node serialized at CreateLiveCachedComponent time.
    public SerializedRenderFragment GetSerializedRenderFragment()
    {
        var nodes = new List<RenderTreeNode>(_entries.Count);
        long contentSize = 0;

        foreach (var entry in _entries)
        {
            if (entry.LiveCachedComponentNode is { } liveCachedComponentNode)
            {
                // Live components are component nodes, not strings; charge a flat overhead so
                // live-cached-component-heavy boundaries still count toward the cache size limit.
                contentSize += LiveCachedComponentSizeEstimate;
                nodes.Add(liveCachedComponentNode);
            }
            else
            {
                contentSize += entry.Markup?.Length ?? 0;
                nodes.Add(new RenderTreeNode { Type = "markup", Content = entry.Markup });
            }
        }

        return new SerializedRenderFragment { Nodes = nodes, ContentSize = contentSize };
    }

    // A live cached component is serialized from its parent's frames, which carry no nested RenderFragment captures, so a
    // RenderFragment parameter could not be replayed correctly. Surface an actionable error before the
    // generic serializer error fires.
    private static void ThrowIfLiveCachedComponentHasRenderFragmentParameter(Type liveCachedComponentType, RenderFragmentCapture capture)
    {
        foreach (ref readonly var frame in capture.GetCapturedFrames().AsSpan())
        {
            if (frame.FrameType is RenderTreeFrameType.Attribute && IsRenderFragmentParameter(frame.AttributeValue))
            {
                throw new InvalidOperationException(
                    $"The [CacheBoundaryLiveComponent] live cached component '{liveCachedComponentType.FullName}' cannot be used inside a CacheBoundary because its RenderFragment parameter '{frame.AttributeName}' would be frozen to the first render's content (a live cached component's parameters are captured once and replayed). " +
                    "To fix this, remove the RenderFragment parameter or move the component outside the CacheBoundary.");
            }
        }
    }

    private static bool IsRenderFragmentParameter(object? value)
        => value is RenderFragment || (value is Delegate d && d.GetType().IsGenericType && d.GetType().GetGenericTypeDefinition() == typeof(RenderFragment<>));

    private void FlushBuffer()
    {
        if (_buffer.Length > 0)
        {
            _entries.Add(CacheCaptureEntry.MarkupEntry(_buffer.ToString()));
            _buffer.Clear();
        }
    }
}

internal readonly struct CacheCaptureEntry
{
    public string? Markup { get; }
    public RenderTreeNode? LiveCachedComponentNode { get; }

    private CacheCaptureEntry(string? markup, RenderTreeNode? liveCachedComponentNode)
    {
        Markup = markup;
        LiveCachedComponentNode = liveCachedComponentNode;
    }

    public static CacheCaptureEntry MarkupEntry(string markup)
        => new(markup, liveCachedComponentNode: null);

    public static CacheCaptureEntry LiveCachedComponent(RenderTreeNode liveCachedComponentNode)
        => new(markup: null, liveCachedComponentNode);
}
