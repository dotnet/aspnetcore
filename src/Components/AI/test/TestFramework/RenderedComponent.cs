// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class RenderedComponent<T> where T : IComponent
{
    private readonly TestRenderer _renderer;

    internal RenderedComponent(TestRenderer renderer, int componentId, T instance)
    {
        _renderer = renderer;
        ComponentId = componentId;
        Instance = instance;
    }

    public int ComponentId { get; }

    public T Instance { get; }

    public string GetHtml() => _renderer.GetHtml(ComponentId);

    public ComponentNode GetNode() => _renderer.Tree.GetNode(ComponentId);

    public RenderedComponent<TChild> FindComponent<TChild>() where TChild : IComponent
    {
        var node = _renderer.Tree.FindDescendant<TChild>(ComponentId);
        if (node is null)
        {
            throw new InvalidOperationException(
                $"No component of type {typeof(TChild).Name} found in the tree rooted at {typeof(T).Name}.");
        }

        return new RenderedComponent<TChild>(_renderer, node.ComponentId, (TChild)node.Instance);
    }

    public IReadOnlyList<RenderedComponent<TChild>> FindAllComponents<TChild>()
        where TChild : IComponent
    {
        var rootNode = _renderer.Tree.GetNode(ComponentId);
        var childNodes = rootNode.FindAllDescendants<TChild>();
        var results = new List<RenderedComponent<TChild>>();
        foreach (var node in childNodes)
        {
            results.Add(new RenderedComponent<TChild>(_renderer, node.ComponentId, (TChild)node.Instance));
        }

        return results;
    }

    public Task InvokeAsync(Func<Task> callback)
        => _renderer.Dispatcher.InvokeAsync(callback);

    public Task InvokeAsync(Action callback)
        => _renderer.Dispatcher.InvokeAsync(callback);
}
