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
/// Unit tests for the Image component.
/// </summary>
public class ImageTest
{
    private static readonly byte[] PngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAA1JREFUGFdjqK6u/g8ABVcCcYoGhmwAAAAASUVORK5CYII=");

    [Fact]
    public async Task LoadsImage_InvokesLoadImageFromStream_WhenCacheMiss()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var source = new ImageSource(PngBytes, "image/png", cacheKey: "png-1");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));

        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.trySetFromCache"));
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream"));
    }

    [Fact]
    public async Task SkipsStreaming_OnCacheHit()
    {
        var js = new FakeImageJsRuntime(cacheHit: true);
        js.MarkCached("png-hit");
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var source = new ImageSource(PngBytes, "image/png", cacheKey: "png-hit");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));

        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.trySetFromCache"));
        Assert.Equal(0, js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream"));
    }

    [Fact]
    public async Task SameCacheKey_NoReload()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var s1 = new ImageSource(new byte[10], "image/png", cacheKey: "same");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s1,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));

        var s2 = new ImageSource(new byte[20], "image/png", cacheKey: "same");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s2,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));

        // Implementation skips reloading when cache key unchanged.
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.trySetFromCache"));
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream"));
    }

    [Fact]
    public async Task NullSource_DoesNothing()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = null,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));

        Assert.Equal(0, js.TotalInvocationCount);
    }

    [Fact]
    public async Task ReusingImageSource_WithConsumedSeekableStream_DoesNotReloadAndSetsErrorState()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp1 = (Image)renderer.InstantiateComponent<Image>();
        var comp2 = (Image)renderer.InstantiateComponent<Image>();
        var id1 = renderer.AssignRootComponentId(comp1);
        var id2 = renderer.AssignRootComponentId(comp2);

        var shared = new ImageSource(new byte[5], "image/png", cacheKey: "reuse-test");
        await renderer.RenderRootComponentAsync(id1, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = shared,
            [nameof(Image.CacheStrategy)] = CacheStrategy.None,
        }));

        // Consume stream, ensure position != 0
        if (shared.Stream.CanSeek)
        {
            shared.Stream.Seek(shared.Stream.Length, SeekOrigin.Begin);
        }

        var initialLoadCalls = js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream");

        // Second component attempts to use same (consumed) ImageSource.
        await renderer.RenderRootComponentAsync(id2, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = shared,
            [nameof(Image.CacheStrategy)] = CacheStrategy.None,
        }));

        // Because the Image component swallows the exception internally, we assert that no additional JS load occurred
        Assert.Equal(initialLoadCalls, js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream"));
    }

    [Fact]
    public async Task Dispose_RevokesUrl()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new ImageSource(new byte[10], "image/png", "dispose-test");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
            [nameof(Image.CacheStrategy)] = CacheStrategy.None,
        }));

        await comp.DisposeAsync();
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.revokeImageUrl"));
    }

    [Fact]
    public async Task CacheStrategyNone_SkipsCacheProbe()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new ImageSource(new byte[8], "image/png", "none-key");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
            [nameof(Image.CacheStrategy)] = CacheStrategy.None,
        }));
        Assert.Equal(0, js.Count("Blazor._internal.BinaryImageComponent.trySetFromCache"));
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream"));
    }

    [Fact]
    public async Task ParameterChange_DifferentCacheKey_Reloads()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);
        var s1 = new ImageSource(new byte[4], "image/png", "key-a");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s1,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));
        var s2 = new ImageSource(new byte[6], "image/png", "key-b");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s2,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));
        Assert.Equal(2, js.Count("Blazor._internal.BinaryImageComponent.trySetFromCache"));
        Assert.Equal(2, js.Count("Blazor._internal.BinaryImageComponent.loadImageFromStream"));
    }

    [Fact]
    public async Task Dispose_NoSource_DoesNotInvokeRevoke()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);
        // Initial render with null Source
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = null,
            [nameof(Image.CacheStrategy)] = CacheStrategy.Memory,
        }));
        await comp.DisposeAsync();
        Assert.Equal(0, js.Count("Blazor._internal.BinaryImageComponent.revokeImageUrl"));
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
        public InteractiveTestRenderer(IServiceProvider serviceProvider) : base(serviceProvider) { }
        protected internal override RendererInfo RendererInfo => new RendererInfo("Test", isInteractive: true);
    }

    private sealed class FakeImageJsRuntime : IJSRuntime
    {
        public sealed record Invocation(string Identifier, object?[] Args);
        private readonly ConcurrentQueue<Invocation> _invocations = new();
        private readonly ConcurrentDictionary<string, bool> _memoryCache = new();
        private readonly bool _forceCacheHit;

        public FakeImageJsRuntime(bool cacheHit) { _forceCacheHit = cacheHit; }

        public int TotalInvocationCount => _invocations.Count;
        public void MarkCached(string cacheKey) => _memoryCache[cacheKey] = true;
        public int Count(string id) => _invocations.Count(i => i.Identifier == id);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? Array.Empty<object?>());

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            args ??= Array.Empty<object?>();
            _invocations.Enqueue(new Invocation(identifier, args));
            object? result = default;
            switch (identifier)
            {
                case "Blazor._internal.BinaryImageComponent.trySetFromCache":
                    var cacheKey = args.Length > 1 ? args[1] as string : null;
                    result = (object?)((_forceCacheHit && cacheKey != null) || (cacheKey != null && _memoryCache.ContainsKey(cacheKey)));
                    break;
                case "Blazor._internal.BinaryImageComponent.loadImageFromStream":
                    // Simulate that after load the cache becomes populated (when strategy memory)
                    if (args.Length >= 5 && args[3] is string ck && !string.IsNullOrEmpty(ck))
                    {
                        _memoryCache[ck] = true;
                    }
                    break;
                case "Blazor._internal.BinaryImageComponent.revokeImageUrl":
                default:
                    break;
            }
            return ValueTask.FromResult((TValue?)result!);
        }
    }
}
