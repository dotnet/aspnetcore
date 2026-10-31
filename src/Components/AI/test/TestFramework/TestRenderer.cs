// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class TestRenderer : Renderer
{
    private readonly List<CapturedBatch> _batches = new();
    private readonly ComponentTree _tree;
    private int _batchIndex;

    internal TestRenderer() : this(new TestServiceProvider())
    {
    }

    internal TestRenderer(IServiceProvider serviceProvider)
        : base(serviceProvider, NullLoggerFactory.Instance)
    {
        Dispatcher = Dispatcher.CreateDefault();
        _tree = new ComponentTree(this);
    }

    public override Dispatcher Dispatcher { get; }

    public IReadOnlyList<CapturedBatch> Batches => _batches;

    public ComponentTree Tree => _tree;

    public new int AssignRootComponentId(IComponent component)
    {
        var id = base.AssignRootComponentId(component);
        _tree.AddRoot(id, component);
        return id;
    }

    public void RenderRootComponent(int componentId, ParameterView? parameters = null)
    {
        var task = Dispatcher.InvokeAsync(
            () => base.RenderRootComponentAsync(componentId, parameters ?? ParameterView.Empty));
        UnwrapTask(task);
    }

    public new Task RenderRootComponentAsync(int componentId, ParameterView parameters)
        => Dispatcher.InvokeAsync(() => base.RenderRootComponentAsync(componentId, parameters));

    public Task DispatchEventAsync(ulong eventHandlerId, EventArgs args)
        => Dispatcher.InvokeAsync(() => base.DispatchEventAsync(eventHandlerId, null, args));

    public new ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
        => base.GetCurrentRenderTreeFrames(componentId);

    public RenderedComponent<T> RenderComponent<T>(
        Action<Dictionary<string, object?>>? configure = null) where T : IComponent
    {
        var component = InstantiateComponent(typeof(T));
        var componentId = AssignRootComponentId(component);

        var parameters = new Dictionary<string, object?>();
        configure?.Invoke(parameters);

        RenderRootComponent(componentId, ParameterView.FromDictionary(parameters));

        return new RenderedComponent<T>(this, componentId, (T)component);
    }

    internal string GetHtml(int componentId)
    {
        var writer = new HtmlContentWriter(this);
        var frames = GetCurrentRenderTreeFrames(componentId);
        writer.WriteFrames(frames, 0, frames.Count);
        return writer.GetResult();
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        var batch = new CapturedBatch(_batchIndex);

        for (var i = 0; i < renderBatch.UpdatedComponents.Count; i++)
        {
            ref var diff = ref renderBatch.UpdatedComponents.Array[i];
            batch.AddUpdatedComponent(diff.ComponentId);
        }

        for (var i = 0; i < renderBatch.DisposedComponentIDs.Count; i++)
        {
            batch.AddDisposedComponent(renderBatch.DisposedComponentIDs.Array[i]);
        }

        _batches.Add(batch);

        // Remove disposed components from the tree
        foreach (var id in batch.DisposedComponentIds)
        {
            _tree.RemoveComponent(id);
        }

        // Rebuild parent-child relationships from current render tree state
        _tree.RebuildRelationships();

        // Capture HTML snapshots for each updated component
        foreach (var id in batch.UpdatedComponentIds)
        {
            if (_tree.HasNode(id))
            {
                var html = GetHtml(id);
                _tree.GetNode(id).AddRender(_batchIndex, html);
            }
        }

        _batchIndex++;
        return Task.CompletedTask;
    }

    protected override void HandleException(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    private static void UnwrapTask(Task task)
    {
        Assert.True(task.IsCompleted);
        if (task.IsFaulted)
        {
            var exception = task.Exception!.Flatten().InnerException!;
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
