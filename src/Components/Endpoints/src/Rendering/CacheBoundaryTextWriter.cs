// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CacheBoundaryTextWriter : TextWriter
{
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

    public void CreateHole(Type componentType, IComponentRenderMode? renderMode, RenderFragmentCapture capture, ILogger renderFragmentSerializationLogger)
    {
        ThrowIfHoleHasRenderFragmentParameter(componentType, capture);

        RenderTreeNode? holeNode = null;
        foreach (var node in RenderFragmentSerializer.SerializeFrames(capture, renderFragmentSerializationLogger))
        {
            if (node.Type is "component")
            {
                holeNode = node;
                break;
            }
        }

        if (holeNode is null)
        {
            throw new InvalidOperationException(
                $"CacheBoundary could not serialize the hole component '{componentType.FullName}' from its parent's render tree.");
        }

        // The serializer fills RenderModeName from an inline @rendermode frame. For components that
        // declare their render mode via [RenderModeAttribute] instead, the capture has no render-mode
        // frame, so patch it from the boundary's runtime render mode.
        var renderModeName = RenderFragmentSerializer.GetRenderModeName(renderMode);
        if (renderModeName is not null && holeNode.RenderModeName is null)
        {
            holeNode.RenderModeName = renderModeName;
            holeNode.Prerender = RenderFragmentSerializer.GetRenderModePrerender(renderMode);
        }

        _entries.Add(CacheCaptureEntry.Hole(holeNode));
    }

    public void StopCapture()
    {
        _capturing = false;
        FlushBuffer();
    }

    // Assembles the cache JSON by walking the recorded entries in render order: markup segments become
    // markup nodes and holes contribute the component node serialized at CreateHole time.
    public string GetJson()
    {
        var nodes = new List<RenderTreeNode>(_entries.Count);

        foreach (var entry in _entries)
        {
            nodes.Add(entry.HoleNode ?? new RenderTreeNode { Type = "markup", Content = entry.Markup });
        }

        return JsonSerializer.Serialize(
            new SerializedRenderFragment { Nodes = nodes },
            ServerComponentSerializationSettings.JsonSerializationOptions);
    }

    // A hole is serialized from its parent's frames, which carry no nested RenderFragment captures, so a
    // RenderFragment parameter could not be replayed correctly. Surface an actionable error before the
    // generic serializer error fires.
    private static void ThrowIfHoleHasRenderFragmentParameter(Type holeComponentType, RenderFragmentCapture capture)
    {
        foreach (ref readonly var frame in capture.GetCapturedFrames().AsSpan())
        {
            if (frame.FrameType is RenderTreeFrameType.Attribute && IsRenderFragmentParameter(frame.AttributeValue))
            {
                throw new InvalidOperationException(
                    $"The [CacheBoundaryPolicy] hole component '{holeComponentType.FullName}' cannot be used inside a CacheBoundary because its RenderFragment parameter" +
                    "'{frame.AttributeName}' would be frozen to the first render's content (a hole's parameters are captured once and replayed). " +
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
    public RenderTreeNode? HoleNode { get; }

    private CacheCaptureEntry(string? markup, RenderTreeNode? holeNode)
    {
        Markup = markup;
        HoleNode = holeNode;
    }

    public static CacheCaptureEntry MarkupEntry(string markup)
        => new(markup, holeNode: null);

    public static CacheCaptureEntry Hole(RenderTreeNode holeNode)
        => new(markup: null, holeNode);
}
