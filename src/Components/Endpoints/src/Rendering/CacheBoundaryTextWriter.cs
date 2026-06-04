// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CacheBoundaryTextWriter : TextWriter
{
    private readonly TextWriter _innerWriter;
    private readonly StringBuilder _buffer = new();
    private readonly List<CacheCaptureEntry> _entries = [];
    private bool _capturing;
    private bool _hasHoles;

    public CacheBoundaryTextWriter(TextWriter inner, CacheBoundaryVaryBy varyBy, RenderFragmentCapture? capture)
    {
        _innerWriter = inner;
        VaryBy = varyBy;
        Capture = capture;
    }

    public CacheBoundaryVaryBy VaryBy { get; set; }

    public bool IsCapturing => _capturing;

    public RenderFragmentCapture? Capture { get; }

    public override Encoding Encoding => _innerWriter.Encoding;

    public override void Write(char value)
    {
        _innerWriter.Write(value);
        if (_capturing)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        _innerWriter.Write(value);
        if (_capturing)
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

    public void CreateHole(Type componentType, int sequence, object? componentKey, string? renderModeName)
    {
        _entries.Add(CacheCaptureEntry.Hole(componentType, sequence, componentKey, renderModeName));
        _hasHoles = true;
    }

    public void StopCapture()
    {
        _capturing = false;
        FlushBuffer();
    }

    /// Produces the cache JSON for the captured render.
    /// 1. Serialize the captured ChildContent frames into a component tree.
    /// 2. Index all component nodes by (TypeName, Sequence) for fast lookup.
    /// 3. Walk entries in render order, emitting markup nodes directly and resolving hole entries against the index.
    public string GetJson(ILogger renderFragmentSerializationLogger)
    {
        Dictionary<(string, int), Queue<RenderTreeNode>>? componentIndex = null;

        if (_hasHoles)
        {
            if (Capture is null)
            {
                throw new InvalidOperationException("CacheBoundary captured holes but no ChildContent capture was provided.");
            }

            var serializedNodes = RenderFragmentSerializer.SerializeFrames(Capture, renderFragmentSerializationLogger);
            componentIndex = IndexComponentNodes(serializedNodes);
        }

        var nodes = new List<RenderTreeNode>(_entries.Count);

        foreach (var entry in _entries)
        {
            if (!entry.IsHole)
            {
                nodes.Add(new RenderTreeNode { Type = "markup", Content = entry.Markup });
                continue;
            }

            var key = (entry.ComponentType!.FullName!, entry.Sequence);
            if (!componentIndex!.TryGetValue(key, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException(
                    $"CacheBoundary could not locate component '{key.Item1}' (sequence {key.Item2}) in the serialized ChildContent tree. " +
                    "This happens when a component marked with [CacheBoundaryPolicy] (a \"hole\") is emitted by an intermediate component's own BuildRenderTree rather than appearing directly inside the CacheBoundary's ChildContent or inside a RenderFragment parameter passed through it. " +
                    "To fix this, either move the hole component so that it is a direct child of the CacheBoundary's content (or passed as a RenderFragment parameter), or annotate the intermediate wrapper component itself with [CacheBoundaryPolicy] so its entire subtree is treated as a hole.");
            }

            var componentNode = queue.Dequeue();

            // The serializer fills RenderModeName from an inline @rendermode frame in the capture.
            // For components that declare their render mode via [RenderModeAttribute] instead,
            // the capture has no ComponentRenderMode frame, so we patch it from the runtime value.
            if (entry.RenderModeName is not null && componentNode.RenderModeName is null)
            {
                componentNode.RenderModeName = entry.RenderModeName;
            }

            nodes.Add(componentNode);
        }

        return JsonSerializer.Serialize(
            new SerializedRenderFragment { Nodes = nodes },
            ServerComponentSerializationSettings.JsonSerializationOptions);
    }

    private void FlushBuffer()
    {
        if (_buffer.Length > 0)
        {
            _entries.Add(CacheCaptureEntry.MarkupEntry(_buffer.ToString()));
            _buffer.Clear();
        }
    }

    /// Builds a lookup of all component nodes in the serialized tree, keyed by (TypeName, Sequence).
    /// Descends into element children and into serialized RenderFragment parameter values so that
    /// components nested inside wrapper components (e.g., CascadingValue) are reachable.
    private static Dictionary<(string, int), Queue<RenderTreeNode>> IndexComponentNodes(List<RenderTreeNode> nodes)
    {
        var index = new Dictionary<(string, int), Queue<RenderTreeNode>>();
        IndexComponentNodesCore(nodes, index);
        return index;
    }

    private static void IndexComponentNodesCore(List<RenderTreeNode> nodes, Dictionary<(string, int), Queue<RenderTreeNode>> index)
    {
        foreach (var node in nodes)
        {
            if (node.Type is "component")
            {
                if (node.TypeName is not null && node.Sequence is { } seq)
                {
                    var key = (node.TypeName, seq);
                    if (!index.TryGetValue(key, out var queue))
                    {
                        queue = new Queue<RenderTreeNode>();
                        index[key] = queue;
                    }
                    // We use queue here, because in recursive RenderFragments it's possible to have multiple nodes with the same (TypeName, Sequence) key, and they must be dequeued in the order they appear in the render.
                    // We can be sure that we will have correct node for each hole, because we traverse the tree in render order, and holes are generated in html rendering pipeline in the same render order as well.
                    queue.Enqueue(node);
                }

                if (node.ComponentParameters is { } parameters)
                {
                    foreach (var parameter in parameters)
                    {
                        if (parameter.Value is SerializedRenderFragment nested)
                        {
                            IndexComponentNodesCore(nested.Nodes, index);
                        }
                    }
                }
            }
            else if (node.Children is { Count: > 0 } children)
            {
                IndexComponentNodesCore(children, index);
            }
        }
    }
}

internal readonly struct CacheCaptureEntry
{
    public bool IsHole { get; }
    public string? Markup { get; }
    public Type? ComponentType { get; }
    public int Sequence { get; }
    public object? ComponentKey { get; }
    public string? RenderModeName { get; }

    private CacheCaptureEntry(bool isHole, string? markup, Type? componentType, int sequence, object? componentKey, string? renderModeName)
    {
        IsHole = isHole;
        Markup = markup;
        ComponentType = componentType;
        Sequence = sequence;
        ComponentKey = componentKey;
        RenderModeName = renderModeName;
    }

    public static CacheCaptureEntry MarkupEntry(string markup)
        => new(isHole: false, markup, componentType: null, sequence: 0, componentKey: null, renderModeName: null);

    public static CacheCaptureEntry Hole(Type componentType, int sequence, object? componentKey, string? renderModeName)
        => new(isHole: true, markup: null, componentType, sequence, componentKey, renderModeName);
}
