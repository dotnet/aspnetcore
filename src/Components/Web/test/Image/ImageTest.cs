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
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web.Image.Tests;

/// <summary>
/// Unit tests for the Image component
/// </summary>
public class ImageTest
{
    private static readonly byte[] PngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAA1JREFUGFdjqK6u/g8ABVcCcYoGhmwAAAAASUVORK5CYII=");

    [Fact]
    public async Task LoadsImage_InvokesSetImageAsync_WhenSourceProvided()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var source = new ImageSource(PngBytes, "image/png", cacheKey: "png-1");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = source,
        }));

        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.setImageAsync"));
    }

    [Fact]
    public async Task SkipsReload_OnSameCacheKey()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var s1 = new ImageSource(new byte[10], "image/png", cacheKey: "same");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s1,
        }));

        var s2 = new ImageSource(new byte[20], "image/png", cacheKey: "same");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s2,
        }));

        // Implementation skips reloading when cache key unchanged.
        Assert.Equal(1, js.Count("Blazor._internal.BinaryImageComponent.setImageAsync"));
    }

    [Fact]
    public async Task NullSource_Throws()
    {
        var js = new FakeImageJsRuntime(cacheHit: false);
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Image.Source)] = null,
            }));
        });

        // Ensure no JS interop calls were made
        Assert.Equal(0, js.TotalInvocationCount);
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
        }));
        var s2 = new ImageSource(new byte[6], "image/png", "key-b");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s2,
        }));
        Assert.Equal(2, js.Count("Blazor._internal.BinaryImageComponent.setImageAsync"));
    }

    [Fact]
    public async Task ChangingSource_CancelsPreviousLoad()
    {
        var js = new FakeImageJsRuntime(cacheHit: false) { DelayOnFirstSetCall = true };
        using var renderer = CreateRenderer(js);
        var comp = (Image)renderer.InstantiateComponent<Image>();
        var id = renderer.AssignRootComponentId(comp);

        var s1 = new ImageSource(new byte[10], "image/png", "k1");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s1,
        }));

        var s2 = new ImageSource(new byte[10], "image/png", "k2");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Image.Source)] = s2,
        }));

        // Give a tiny bit of time for cancellation to propagate
        for (var i = 0; i < 10 && js.CapturedTokens.Count < 2; i++)
        {
            await Task.Delay(10);
        }

        Assert.NotEmpty(js.CapturedTokens);
        Assert.True(js.CapturedTokens.First().IsCancellationRequested);

        // Two invocations total (first canceled, second completes)
        Assert.Equal(2, js.Count("Blazor._internal.BinaryImageComponent.setImageAsync"));
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
        public sealed record Invocation(string Identifier, object?[] Args, CancellationToken Token);
        private readonly ConcurrentQueue<Invocation> _invocations = new();
        private readonly ConcurrentDictionary<string, bool> _memoryCache = new();
        private readonly bool _forceCacheHit;

        public FakeImageJsRuntime(bool cacheHit) { _forceCacheHit = cacheHit; }

        public int TotalInvocationCount => _invocations.Count;
        public int Count(string id) => _invocations.Count(i => i.Identifier == id);
        public IReadOnlyList<CancellationToken> CapturedTokens => _invocations.Select(i => i.Token).ToList();
        public void MarkCached(string cacheKey) => _memoryCache[cacheKey] = true;

        // Simulation flags
        public bool DelayOnFirstSetCall { get; set; }
        public bool ForceFail { get; set; }
        public bool FailOnce { get; set; } = true;
        public bool FailIfTotalBytesIsZero { get; set; }
        private bool _failUsed;
        private int _setCalls;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? Array.Empty<object?>());

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            args ??= Array.Empty<object?>();
            _invocations.Enqueue(new Invocation(identifier, args, cancellationToken));

            if (identifier == "Blazor._internal.BinaryImageComponent.setImageAsync")
            {
                _setCalls++;
                var cacheKey = args.Length >= 4 ? args[3] as string : null;
                var hasStream = args.Length >= 2 && args[1] != null;
                long? totalBytes = null;
                if (args.Length >= 5 && args[4] != null)
                {
                    try { totalBytes = Convert.ToInt64(args[4], System.Globalization.CultureInfo.InvariantCulture); } catch { totalBytes = null; }
                }

                if (DelayOnFirstSetCall && _setCalls == 1)
                {
                    var tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
                    cancellationToken.Register(() => tcs.TrySetException(new OperationCanceledException(cancellationToken)));
                    return new ValueTask<TValue>(tcs.Task);
                }

                var shouldFail = (ForceFail && (!_failUsed || !FailOnce))
                                 || (FailIfTotalBytesIsZero && (totalBytes.HasValue && totalBytes.Value == 0));
                if (ForceFail)
                {
                    _failUsed = true;
                }

                var fromCache = !shouldFail && cacheKey != null && (_forceCacheHit || _memoryCache.ContainsKey(cacheKey));
                if (!fromCache && hasStream && !string.IsNullOrEmpty(cacheKey) && !shouldFail)
                {
                    _memoryCache[cacheKey!] = true;
                }

                var t = typeof(TValue);
                object? instance = Activator.CreateInstance(t, nonPublic: true);
                if (instance is null)
                {
                    return ValueTask.FromResult(default(TValue)!);
                }

                var setProp = (string name, object? value) =>
                {
                    var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    p?.SetValue(instance, value);
                };

                if (shouldFail)
                {
                    setProp("Success", false);
                    setProp("FromCache", false);
                    setProp("ObjectUrl", null);
                    setProp("Error", "simulated-failure");
                }
                else
                {
                    setProp("Success", hasStream || fromCache);
                    setProp("FromCache", fromCache);
                    setProp("ObjectUrl", (hasStream || fromCache) && cacheKey != null ? $"blob:{cacheKey}:{Guid.NewGuid()}" : null);
                    setProp("Error", null);
                }

                return ValueTask.FromResult((TValue)instance);
            }

            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
