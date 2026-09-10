// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class ComponentNode
{
    private readonly List<ComponentNode> _children = new();
    private readonly List<ComponentRender> _renders = new();

    internal ComponentNode(int componentId, Type componentType, IComponent instance)
    {
        ComponentId = componentId;
        ComponentType = componentType;
        Instance = instance;
    }

    public int ComponentId { get; }

    public Type ComponentType { get; }

    public IComponent Instance { get; }

    public ComponentNode? Parent { get; internal set; }

    public IReadOnlyList<ComponentNode> Children => _children;

    public IReadOnlyList<ComponentRender> Renders => _renders;

    public int RenderCount => _renders.Count;

    internal void AddChild(ComponentNode child)
    {
        _children.Add(child);
    }

    internal void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.Parent = null;
        }

        _children.Clear();
    }

    internal void AddRender(int batchIndex, string html)
    {
        _renders.Add(new ComponentRender(batchIndex, html));
    }

    public ComponentNode? FindDescendant<T>() where T : IComponent
    {
        foreach (var child in _children)
        {
            if (child.Instance is T)
            {
                return child;
            }

            var found = child.FindDescendant<T>();
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public IReadOnlyList<ComponentNode> FindAllDescendants<T>() where T : IComponent
    {
        var results = new List<ComponentNode>();
        CollectDescendants<T>(results);
        return results;
    }

    private void CollectDescendants<T>(List<ComponentNode> results) where T : IComponent
    {
        foreach (var child in _children)
        {
            if (child.Instance is T)
            {
                results.Add(child);
            }

            child.CollectDescendants<T>(results);
        }
    }
}
