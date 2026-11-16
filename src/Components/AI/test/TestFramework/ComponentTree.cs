// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class ComponentTree
{
    private readonly TestRenderer _renderer;
    private readonly Dictionary<int, ComponentNode> _nodes = new();
    private readonly List<int> _rootIds = new();

    internal ComponentTree(TestRenderer renderer)
    {
        _renderer = renderer;
    }

    public IReadOnlyList<ComponentNode> Roots
    {
        get
        {
            var roots = new List<ComponentNode>();
            foreach (var id in _rootIds)
            {
                if (_nodes.TryGetValue(id, out var node))
                {
                    roots.Add(node);
                }
            }

            return roots;
        }
    }

    internal void AddRoot(int componentId, IComponent component)
    {
        _rootIds.Add(componentId);
        _nodes[componentId] = new ComponentNode(componentId, component.GetType(), component);
    }

    internal void RemoveComponent(int componentId)
    {
        _nodes.Remove(componentId);
        _rootIds.Remove(componentId);
    }

    internal bool HasNode(int componentId) => _nodes.ContainsKey(componentId);

    internal ComponentNode GetNode(int componentId) => _nodes[componentId];

    internal ComponentNode? FindDescendant<T>(int rootComponentId) where T : IComponent
    {
        if (_nodes.TryGetValue(rootComponentId, out var node))
        {
            // Check the node itself first
            if (node.Instance is T)
            {
                return node;
            }

            return node.FindDescendant<T>();
        }

        return null;
    }

    internal void RebuildRelationships()
    {
        // Clear all parent-child relationships
        foreach (var node in _nodes.Values)
        {
            node.ClearChildren();
        }

        // Rebuild from roots by scanning current render tree frames
        foreach (var rootId in _rootIds)
        {
            if (_nodes.TryGetValue(rootId, out var rootNode))
            {
                ScanChildren(rootNode);
            }
        }
    }

    private void ScanChildren(ComponentNode parentNode)
    {
        var frames = _renderer.GetCurrentRenderTreeFrames(parentNode.ComponentId);
        ScanFramesForChildren(frames, 0, frames.Count, parentNode);
    }

    private void ScanFramesForChildren(
        ArrayRange<RenderTreeFrame> frames,
        int start,
        int end,
        ComponentNode parentNode)
    {
        var i = start;
        while (i < end)
        {
            ref var frame = ref frames.Array[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Component:
                    var childId = frame.ComponentId;
                    if (!_nodes.TryGetValue(childId, out var childNode))
                    {
                        childNode = new ComponentNode(childId, frame.ComponentType, frame.Component);
                        _nodes[childId] = childNode;
                    }

                    parentNode.AddChild(childNode);
                    childNode.Parent = parentNode;

                    // Recurse into child component's own render tree
                    ScanChildren(childNode);
                    i += frame.ComponentSubtreeLength;
                    break;

                case RenderTreeFrameType.Element:
                    // Scan inside elements for nested components
                    ScanFramesForChildren(frames, i + 1, i + frame.ElementSubtreeLength, parentNode);
                    i += frame.ElementSubtreeLength;
                    break;

                case RenderTreeFrameType.Region:
                    ScanFramesForChildren(frames, i + 1, i + frame.RegionSubtreeLength, parentNode);
                    i += frame.RegionSubtreeLength;
                    break;

                default:
                    i++;
                    break;
            }
        }
    }
}
