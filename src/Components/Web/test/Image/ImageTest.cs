// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Components.Web.Image.Tests;

/// <summary>
/// Unit tests for the Image component. These tests focus on component logic and JS interop contracts.
/// </summary>
public class ImageTest
{
    private static readonly byte[] PngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAA1JREFUGFdjqK6u/g8ABVcCcYoGhmwAAAAASUVORK5CYII=");
    private static readonly byte[] JpgBytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/wAALCAABAAEBAREA/8QAFAABAAAAAAAAAAAAAAAAAAAAAP/EABQQAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQEAAD8AN//Z");

    [Fact]
    public async Task StreamsBytesInChunks_AndFinalizes_OnCacheMiss()
    {
        var js = new FakeImageJsRuntime();

        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var source = new ImageSource(PngBytes, "image/png", cacheKey: "png-1");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
            [nameof(Image.ChunkSize)] = 8,
        }));

        // Wait until finalize observed
        await js.WaitUntilAsync(() => js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer") == 1);

        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.initChunkedTransfer"));
        Assert.True(js.Count("Blazor._internal.BinaryImageComponent.addChunk") >= 1);
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer"));

        // Cache populated after finalize for memory strategy
        Assert.True(await js.TrySetFromCacheAsync(source.CacheKey!));

        // Validate transferred total bytes match payload
        var totalBytes = js.TotalTransferredBytes();
        Assert.Equal(PngBytes.Length, totalBytes);
    }

    [Fact]
    public async Task SkipsStreaming_WhenCacheHit()
    {
        var js = new FakeImageJsRuntime();

        // Pre-populate cache for key
        js.MarkCached("png-hit");

        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var source = new ImageSource(PngBytes, "image/png", cacheKey: "png-hit");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
            [nameof(Image.ChunkSize)] = 16,
        }));

        // Should have attempted cache first
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.trySetFromCache"));
        // And not stream if cache hit
        Assert.Equal(0, js.Count("Blazor._internal.BinaryImageComponent.initChunkedTransfer"));
        Assert.Equal(0, js.Count("Blazor._internal.BinaryImageComponent.addChunk"));
        Assert.Equal(0, js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer"));
    }

    [Fact]
    public async Task MultipleComponents_SameImageSource_StreamIndependently()
    {
        var js = new FakeImageJsRuntime();

        using var renderer = CreateRenderer(js);

        var comp1 = (Image)renderer.InstantiateComponent<Image>();
        var comp2 = (Image)renderer.InstantiateComponent<Image>();
        var id1 = renderer.AssignRootComponentId(comp1);
        var id2 = renderer.AssignRootComponentId(comp2);

        var shared = new ImageSource(JpgBytes, "image/jpeg", cacheKey: "shared-key");

        await Task.WhenAll(
            renderer.RenderRootComponentAsync(id1, ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Image.Source)] = shared,
                [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
                [nameof(Image.ChunkSize)] = 8,
            })),
            renderer.RenderRootComponentAsync(id2, ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Image.Source)] = shared,
                [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
                [nameof(Image.ChunkSize)] = 8,
            }))
        );

        // Eventually either: stream twice, or first streams and second hits cache
        await js.WaitUntilAsync(() => js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer") >= 1);

        var inits = js.Count("Blazor._internal.BinaryImageComponent.initChunkedTransfer");
        var finalizes = js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer");
        Assert.InRange(inits, 1, 2);
        Assert.InRange(finalizes, 1, 2);

        // Either way, the cache should end up populated
        Assert.True(await js.TrySetFromCacheAsync("shared-key"));
    }

    [Fact]
    public async Task ChangingSource_CancelsInFlightStreaming_NoFinalizeForOldLoad()
    {
        var js = new FakeImageJsRuntime();

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var addChunkEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        js.BlockFirstAddChunk(gate, addChunkEntered);

        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var first = new ImageSource(new byte[64], "image/png", cacheKey: "first");
        var second = new ImageSource(new byte[32], "image/png", cacheKey: "second");

        var initialRender = renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = first,
            [nameof(Image.CacheStrategy)] = CacheStrategy.None,
            [nameof(Image.ChunkSize)] = 4,
        }));

        // Wait until the first addChunk starts
        await addChunkEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Change Source while first load is in progress
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = second,
            [nameof(Image.CacheStrategy)] = CacheStrategy.None,
            [nameof(Image.ChunkSize)] = 4,
        }));

        // Allow the first addChunk to continue; the component should notice the version change and not finalize the old transfer
        gate.SetResult();

        // Wait for the new finalize (second load) to happen
        await js.WaitUntilAsync(() => js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer") >= 1);

        // Assert: there was exactly one finalize, for the second load
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer"));
    }

    [Fact]
    public async Task SameCacheKey_NoReloadOnSetParameters()
    {
        var js = new FakeImageJsRuntime();

        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var s1 = new ImageSource(new byte[10], "image/png", cacheKey: "same");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s1,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
            [nameof(Image.ChunkSize)] = 8,
        }));
        await js.WaitUntilAsync(() => js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer") == 1);

        // Change to a different data but same cache key
        var s2 = new ImageSource(new byte[20], "image/png", cacheKey: "same");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s2,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
            [nameof(Image.ChunkSize)] = 8,
        }));

        // No additional streaming should occur because cache key didn't change
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.initChunkedTransfer"));
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer"));
    }

    [Fact]
    public async Task NullSource_DoesNothing()
    {
        var js = new FakeImageJsRuntime();

        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = null,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
            [nameof(Image.ChunkSize)] = 8,
        }));

        Assert.Equal(0, js.TotalInvocationCount);
    }

    private static TestRenderer CreateRenderer(IJSRuntime js)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(js);
        return new InteractiveTestRenderer(services.BuildServiceProvider());
    }

    private sealed class InteractiveTestRenderer : TestRenderer
    {
        public InteractiveTestRenderer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected internal override RendererInfo RendererInfo => new RendererInfo("Test", isInteractive: true);
    }

    private sealed class FakeImageJsRuntime : IJSRuntime
    {
        public sealed record Invocation(string Identifier, object?[] Args);

        private readonly ConcurrentQueue<Invocation> _invocations = new();
        private readonly ConcurrentDictionary<string, (string cacheKey, string strategy, long? totalBytes)> _transfers = new();
        private readonly ConcurrentDictionary<string, bool> _memoryCache = new();

        // For cancellation test
        private TaskCompletionSource? _blockAddChunkGate;
        private TaskCompletionSource? _addChunkEntered;
        private int _blockAddChunkOnce = 0;

        public int TotalInvocationCount => _invocations.Count;

        public void MarkCached(string cacheKey) => _memoryCache[cacheKey] = true;

        public Task<bool> TrySetFromCacheAsync(string cacheKey)
            => Task.FromResult(_memoryCache.ContainsKey(cacheKey));

        public int Count(string id) => _invocations.Count(i => i.Identifier == id);

        public int TotalTransferredBytes()
        {
            // Aggregate from addChunk payload lengths across all transfers
            // Each addChunk args: [transferId, byte[]]
            var addChunks = _invocations.Where(i => i.Identifier == "Blazor._internal.BinaryImageComponent.addChunk");
            var sum = 0;
            foreach (var inv in addChunks)
            {
                var data = inv.Args[1] as byte[] ?? Array.Empty<byte>();
                sum += data.Length;
            }
            return sum;
        }

        public void BlockFirstAddChunk(TaskCompletionSource gate, TaskCompletionSource entered)
        {
            _blockAddChunkGate = gate;
            _addChunkEntered = entered;
            _blockAddChunkOnce = 1;
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => await InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? Array.Empty<object?>());

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            args ??= Array.Empty<object?>();
            _invocations.Enqueue(new Invocation(identifier, args));

            switch (identifier)
            {
                case "Blazor._internal.BinaryImageComponent.trySetFromCache":
                {
                    var cacheKey = args[1] as string;
                    var hit = cacheKey != null && _memoryCache.ContainsKey(cacheKey);
                    return (TValue)(object)hit;
                }
                case "Blazor._internal.BinaryImageComponent.initChunkedTransfer":
                {
                    // args: [ElementReference, transferId, mime, cacheKey, strategy, totalBytes]
                    var transferId = (string)args[1]!;
                    var cacheKey = args[3] as string ?? string.Empty;
                    var strategy = args[4] as string ?? string.Empty;
                    var totalBytes = (args[5] as long?) ?? (args[5] is IConvertible c ? c.ToInt64(null) : (long?)null);
                    _transfers[transferId] = (cacheKey, strategy, totalBytes);
                    return default!;
                }
                case "Blazor._internal.BinaryImageComponent.addChunk":
                {
                    // args: [transferId, byte[]]
                    if (Interlocked.Exchange(ref _blockAddChunkOnce, 0) == 1 && _blockAddChunkGate is not null && _addChunkEntered is not null)
                    {
                        _addChunkEntered.TrySetResult();
                        await _blockAddChunkGate.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    return default!;
                }
                case "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer":
                {
                    // args: [transferId]
                    var transferId = (string)args[0]!;
                    if (_transfers.TryGetValue(transferId, out var info))
                    {
                        if (string.Equals(info.strategy, "memory", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(info.cacheKey))
                        {
                            _memoryCache[info.cacheKey] = true;
                        }
                    }
                    return default!;
                }
                case "Blazor._internal.BinaryImageComponent.clearCache":
                {
                    _memoryCache.Clear();
                    return (TValue)(object)true;
                }
                case "Blazor._internal.BinaryImageComponent.revokeImageUrl":
                default:
                    return default!;
            }
        }

        public async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollMs = 20)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                {
                    return;
                }
                await Task.Delay(pollMs);
            }
            throw new TimeoutException("Condition not met in allotted time.");
        }
    }
}
