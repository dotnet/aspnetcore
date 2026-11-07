// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Performance;

public class KeyedListDiffBenchmark
{
    private Renderer _renderer;
    private RenderBatchBuilder _batchBuilder;
    private RenderTreeBuilder _original;
    private RenderTreeBuilder _sameOrder;
    private RenderTreeBuilder _reordered;

    [Params(500)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _renderer = new FakeRenderer();
        _batchBuilder = new RenderBatchBuilder();

        var keys = Enumerable.Range(0, ItemCount).Select(i => (object)i).ToArray();
        _original = Build(keys);
        _sameOrder = Build(keys);

        // Move a few items and remove/insert some to exercise the move/insert/delete paths
        var reorderedKeys = keys.ToList();
        (reorderedKeys[1], reorderedKeys[ItemCount - 2]) = (reorderedKeys[ItemCount - 2], reorderedKeys[1]);
        reorderedKeys.RemoveAt(ItemCount / 2);
        reorderedKeys.Insert(ItemCount / 4, ItemCount + 1);
        _reordered = Build(reorderedKeys);
    }

    private static RenderTreeBuilder Build(IEnumerable<object> keys)
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "ul");
        foreach (var key in keys)
        {
            builder.OpenElement(1, "li");
            builder.SetKey(key);
            builder.AddAttribute(2, "class", "item");
            builder.AddContent(3, "Item");
            builder.CloseElement();
        }
        builder.CloseElement();
        return builder;
    }

    [Benchmark]
    public void ComputeDiff_KeyedList_SameOrder()
    {
        _batchBuilder.ClearStateForCurrentBatch();
        var diff = RenderTreeDiffBuilder.ComputeDiff(_renderer, _batchBuilder, 0, _original.GetFrames(), _sameOrder.GetFrames());
        GC.KeepAlive(diff);
    }

    [Benchmark]
    public void ComputeDiff_KeyedList_Reordered()
    {
        _batchBuilder.ClearStateForCurrentBatch();
        var diff = RenderTreeDiffBuilder.ComputeDiff(_renderer, _batchBuilder, 0, _original.GetFrames(), _reordered.GetFrames());
        GC.KeepAlive(diff);
    }

    private sealed class FakeRenderer : Renderer
    {
        public FakeRenderer()
            : base(new TestServiceProvider(), NullLoggerFactory.Instance)
        {
        }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override void HandleException(Exception exception) => throw new NotImplementedException();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => null;
    }
}
