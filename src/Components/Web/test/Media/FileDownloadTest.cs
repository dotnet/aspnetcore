// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web.Media.Tests;

/// <summary>
/// Unit tests for <see cref="FileDownload"/> focusing only on behaviors not covered by Image/Video tests.
/// </summary>
public class FileDownloadTest
{
    private static readonly byte[] SampleBytes = new byte[] { 1, 2, 3, 4, 5 };

    [Fact]
    public async Task InitialRender_DoesNotInvokeJs()
    {
        var js = new FakeDownloadJsRuntime();
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);

        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = new MediaSource(SampleBytes, "application/octet-stream", "file-init"),
            [nameof(FileDownload.FileName)] = "first.bin"
        }));

        Assert.Equal(0, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
    }

    [Fact]
    public async Task Click_InvokesDownloadOnce()
    {
        var js = new FakeDownloadJsRuntime { Result = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        await renderer.RenderRootComponentAsync(id, Params("file-click", "ok.bin"));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(1, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
        Assert.False(HasDataState(renderer, id, "error"));
    }

    [Fact]
    public async Task BlankFileName_SuppressesDownload()
    {
        var js = new FakeDownloadJsRuntime { Result = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        await renderer.RenderRootComponentAsync(id, Params("file-noname", "   "));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(0, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
    }

    [Fact]
    public async Task JsReturnsFalse_SetsErrorState()
    {
        var js = new FakeDownloadJsRuntime { Result = false };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        await renderer.RenderRootComponentAsync(id, Params("file-false", "fail.bin"));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(1, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
        Assert.True(HasDataState(renderer, id, "error"));
    }

    [Fact]
    public async Task JsThrows_SetsErrorState()
    {
        var js = new FakeDownloadJsRuntime { Throw = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        await renderer.RenderRootComponentAsync(id, Params("file-throw", "throws.bin"));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(1, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
        Assert.True(HasDataState(renderer, id, "error"));
    }

    [Fact]
    public async Task SecondClick_CancelsFirst()
    {
        var js = new FakeDownloadJsRuntime { DelayOnFirst = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        await renderer.RenderRootComponentAsync(id, Params("file-cancel", "cancel.bin"));

        var first = ClickAnchorAsync(renderer, id); // starts first (will delay)
        await ClickAnchorAsync(renderer, id);       // second click immediately
        await first;                                // allow completion

        Assert.Equal(2, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
        Assert.True(js.CapturedTokens.First().IsCancellationRequested);
        Assert.False(js.CapturedTokens.Last().IsCancellationRequested);
    }

    [Fact]
    public async Task ProvidedHref_IsRemoved_InertHrefUsed()
    {
        var js = new FakeDownloadJsRuntime();
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);

        var attrs = new Dictionary<string, object?> { ["href"] = "https://example.org/real", ["class"] = "btn" };
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = new MediaSource(SampleBytes, "application/octet-stream", "file-href"),
            [nameof(FileDownload.FileName)] = "href.bin",
            [nameof(FileDownload.AdditionalAttributes)] = attrs
        }));

        var frames = renderer.GetCurrentRenderTreeFrames(id);
        var anchorIndex = FindAnchorIndex(frames);
        Assert.True(anchorIndex >= 0, "anchor not found");
        var href = GetAttributeValue(frames, anchorIndex, "href");
        var @class = GetAttributeValue(frames, anchorIndex, "class");
        Assert.Equal("javascript:void(0)", href);
        Assert.Equal("btn", @class);
    }

    // Helpers
    private static ParameterView Params(string key, string fileName) => ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(FileDownload.Source)] = new MediaSource(SampleBytes, "application/octet-stream", key),
        [nameof(FileDownload.FileName)] = fileName
    });

    private static async Task ClickAnchorAsync(TestRenderer renderer, int componentId)
    {
        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        var anchorIndex = FindAnchorIndex(frames);
        Assert.True(anchorIndex >= 0, "anchor not found");
        ulong? handlerId = null;
        for (var i = anchorIndex + 1; i < frames.Count; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute)
            {
                if (frame.AttributeName == "onclick")
                {
                    handlerId = frame.AttributeEventHandlerId;
                }

                continue;
            }
            break;
        }
        Assert.True(handlerId.HasValue, "onclick handler not found");
        await renderer.DispatchEventAsync(handlerId.Value, new MouseEventArgs());
    }

    private static bool HasDataState(TestRenderer renderer, int componentId, string state)
    {
        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        var anchorIndex = FindAnchorIndex(frames);
        if (anchorIndex < 0)
        {
            return false;
        }

        var value = GetAttributeValue(frames, anchorIndex, "data-state");
        return string.Equals(value, state, StringComparison.Ordinal);
    }

    private static int FindAnchorIndex(ArrayRange<RenderTreeFrame> frames)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            ref readonly var f = ref frames.Array[i];
            if (f.FrameType == RenderTreeFrameType.Element && string.Equals(f.ElementName, "a", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private static string? GetAttributeValue(ArrayRange<RenderTreeFrame> frames, int elementIndex, string name)
    {
        for (var i = elementIndex + 1; i < frames.Count; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute)
            {
                if (string.Equals(frame.AttributeName, name, StringComparison.Ordinal))
                {
                    return frame.AttributeValue?.ToString();
                }
                continue;
            }
            break;
        }
        return null;
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
        public InteractiveTestRenderer(IServiceProvider services) : base(services) { }
        protected internal override RendererInfo RendererInfo => new RendererInfo("Test", isInteractive: true);
    }

    private sealed class FakeDownloadJsRuntime : IJSRuntime
    {
        private readonly ConcurrentQueue<Invocation> _invocations = new();
        public bool Result { get; set; } = true;
        public bool Throw { get; set; }
        public bool DelayOnFirst { get; set; }
        private int _calls;

        public IReadOnlyList<CancellationToken> CapturedTokens => _invocations.Select(i => i.Token).ToList();
        public int Count(string id) => _invocations.Count(i => i.Identifier == id);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? Array.Empty<object?>());

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            _invocations.Enqueue(new Invocation(identifier, cancellationToken));
            if (identifier == "Blazor._internal.BinaryMedia.downloadAsync")
            {
                if (Throw)
                {
                    return ValueTask.FromException<TValue>(new InvalidOperationException("Download failed"));
                }
                if (DelayOnFirst && _calls == 0)
                {
                    _calls++;
                    return new ValueTask<TValue>(DelayAsync<TValue>(cancellationToken));
                }
                _calls++;
                object boxed = Result;
                return new ValueTask<TValue>((TValue)boxed);
            }
            return ValueTask.FromException<TValue>(new InvalidOperationException("Unexpected identifier: " + identifier));
        }

        private async Task<TValue> DelayAsync<TValue>(CancellationToken token)
        {
            try { await Task.Delay(50, token); } catch { }
            object boxed = Result;
            return (TValue)boxed;
        }

        private record struct Invocation(string Identifier, CancellationToken Token);
    }
}
