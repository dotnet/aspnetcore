// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class MemoryCacheBoundaryStoreTest
{
    [Fact]
    public async Task GetOrCreateAsync_ConcurrentCallersForSameKey_InvokeFactoryOnce()
    {
        var store = CreateStore();
        var options = new CacheStoreOptions();
        var value = new SerializedRenderFragment { Nodes = [new RenderTreeNode { Type = "markup", Content = "value" }] };

        var factoryInvocations = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<SerializedRenderFragment> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref factoryInvocations);
            await gate.Task;
            return value;
        }

        const int callers = 16;
        var tasks = new Task<SerializedRenderFragment>[callers];
        for (var i = 0; i < callers; i++)
        {
            tasks[i] = store.GetOrCreateAsync("key", Factory, options, default).AsTask();
        }

        // Release the single elected creator; all other callers observe its result.
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, factoryInvocations);
        Assert.All(results, result => Assert.Equal(value, result));
    }

    [Fact]
    public async Task GetOrCreateAsync_AfterCreatorCompletes_ServesCachedValueWithoutFactory()
    {
        var store = CreateStore();
        var options = new CacheStoreOptions();
        var value = new SerializedRenderFragment { Nodes = [new RenderTreeNode { Type = "markup", Content = "value" }] };

        var factoryInvocations = 0;
        ValueTask<SerializedRenderFragment> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref factoryInvocations);
            return ValueTask.FromResult(value);
        }

        var first = await store.GetOrCreateAsync("key", Factory, options, default);
        var second = await store.GetOrCreateAsync("key", Factory, options, default);

        Assert.Equal(value, first);
        Assert.Equal(value, second);
        Assert.Equal(1, factoryInvocations);
    }

    private static MemoryCacheBoundaryStore CreateStore()
        => new(Options.Create(new RazorComponentsServiceOptions()), NullLogger<MemoryCacheBoundaryStore>.Instance);
}
