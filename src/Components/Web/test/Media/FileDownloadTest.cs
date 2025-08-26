// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Media;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web.Media.Tests;

/// <summary>
/// Unit tests for the FileDownload component covering behaviors unique to manual-download semantics.
/// (Auto-load, cache key reuse, etc. are already covered by Image/Video tests.)
/// </summary>
public class FileDownloadTest
{
    private static readonly byte[] SampleBytes = new byte[] { 1, 2, 3, 4, 5 };

    [Fact]
    public async Task DoesNotAutoLoad_OnInitialRender()
    {
        var js = new FakeDownloadJsRuntime();
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);

        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-1");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "test.bin"
        }));

        Assert.Equal(0, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));

        // Verify initial markup contains inert href and marker, no data-state
        var frames = renderer.GetCurrentRenderTreeFrames(id);
        MediaTestUtil.CurrentFrames = frames;
        var a = FindElement(frames, "a");
        Assert.True(a.HasValue);
        MediaTestUtil.CurrentFrames = frames;
        AssertAttribute(a.Value, "href", "javascript:void(0)");
        AssertAttribute(a.Value, "data-blazor-file-download", string.Empty);
        Assert.False(HasAttribute(a.Value, "data-state"));
    }

    [Fact]
    public async Task ClickInvokesDownload_Success_NoErrorState()
    {
        var js = new FakeDownloadJsRuntime { Result = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-2");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "ok.bin",
            [nameof(FileDownload.Text)] = "Get it"
        }));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(1, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));

        var frames = renderer.GetCurrentRenderTreeFrames(id);
        MediaTestUtil.CurrentFrames = frames;
        var a = FindElement(frames, "a");
        Assert.True(a.HasValue);
        Assert.False(HasAttribute(a.Value, "data-state"));
        Assert.Equal("Get it", ReadInnerText(frames));
    }

    [Fact]
    public async Task JsReturnsFalse_SetsErrorState()
    {
        var js = new FakeDownloadJsRuntime { Result = false };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-3");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "fail.bin"
        }));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(1, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));

        var a = FindElement(renderer.GetCurrentRenderTreeFrames(id), "a");
        MediaTestUtil.CurrentFrames = renderer.GetCurrentRenderTreeFrames(id);
        Assert.True(a.HasValue);
        AssertAttribute(a.Value, "data-state", "error");
    }

    [Fact]
    public async Task JsThrows_SetsErrorState()
    {
        var js = new FakeDownloadJsRuntime { Throw = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-4");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "throw.bin"
        }));

        await ClickAnchorAsync(renderer, id);

        var a = FindElement(renderer.GetCurrentRenderTreeFrames(id), "a");
        MediaTestUtil.CurrentFrames = renderer.GetCurrentRenderTreeFrames(id);
        Assert.True(a.HasValue);
        AssertAttribute(a.Value, "data-state", "error");
    }

    [Fact]
    public async Task AdditionalHref_IsIgnored_InertHrefUsed()
    {
        var js = new FakeDownloadJsRuntime();
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-5");
        var additional = new Dictionary<string, object?>
        {
            ["href"] = "https://example.com/should-not-navigate",
            ["class"] = "btn"
        };
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "test.bin",
            [nameof(FileDownload.AdditionalAttributes)] = additional
        }));

        var frames = renderer.GetCurrentRenderTreeFrames(id);
        MediaTestUtil.CurrentFrames = frames;
        var a = FindElement(frames, "a");
        Assert.True(a.HasValue);
        AssertAttribute(a.Value, "href", "javascript:void(0)");
        AssertAttribute(a.Value, "class", "btn");
    }

    [Fact]
    public async Task MissingFileName_SuppressesDownload()
    {
        var js = new FakeDownloadJsRuntime();
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-6");
        // Purposely give whitespace filename
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "  "
        }));

        await ClickAnchorAsync(renderer, id);

        Assert.Equal(0, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
    }

    [Fact]
    public async Task SecondClick_CancelsFirst()
    {
        var js = new FakeDownloadJsRuntime { DelayOnFirst = true };
        using var renderer = CreateRenderer(js);
        var comp = (FileDownload)renderer.InstantiateComponent<FileDownload>();
        var id = renderer.AssignRootComponentId(comp);
        var source = new MediaSource(SampleBytes, "application/octet-stream", cacheKey: "file-7");
        await renderer.RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FileDownload.Source)] = source,
            [nameof(FileDownload.FileName)] = "cancel.bin"
        }));

        var click1 = ClickAnchorAsync(renderer, id);
        // Immediately second click
        await ClickAnchorAsync(renderer, id);
        await click1; // Ensure first completes after cancellation attempt

        Assert.Equal(2, js.Count("Blazor._internal.BinaryMedia.downloadAsync"));
        // First token should be cancelled
        Assert.True(js.CapturedTokens.First().IsCancellationRequested);
        Assert.False(js.CapturedTokens.Last().IsCancellationRequested);
    }

    private static async Task ClickAnchorAsync(TestRenderer renderer, int componentId)
    {
        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        var a = FindElement(frames, "a");
        Assert.True(a.HasValue, "Anchor element not found");
        ulong? handlerId = null;
        // Attributes immediately follow the element frame at index a.Value.Index
        for (var i = a.Value.Index + 1; i < frames.Count; i++)
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
            break; // stop after attributes block
        }
        Assert.True(handlerId.HasValue, "onclick handler not found");
        await renderer.DispatchEventAsync(handlerId.Value, new MouseEventArgs());
    }

    private static (int Index, int SequenceIndex, RenderTreeFrame Frame)? FindElement(ArrayRange<RenderTreeFrame> frames, string elementName)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element && string.Equals(frame.ElementName, elementName, StringComparison.OrdinalIgnoreCase))
            {
                return (i, frame.Sequence, frame);
            }
        }
        return null;
    }

    private static void AssertAttribute((int Index, int SequenceIndex, RenderTreeFrame Frame) element, string name, string? expectedValue)
    {
        var framesRange = MediaTestUtil.CurrentFrames ?? throw new InvalidOperationException("Current frames not set");
        for (var i = element.Index + 1; i < framesRange.Count; i++)
        {
            ref readonly var frame = ref framesRange.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute)
            {
                if (string.Equals(frame.AttributeName, name, StringComparison.Ordinal))
                {
                    Assert.Equal(expectedValue, frame.AttributeValue?.ToString());
                    return;
                }
                continue;
            }
            break; // end of attributes
        }
        Assert.Fail($"Attribute '{name}' not found on element '{element.Frame.ElementName}'.");
    }

    private static bool HasAttribute((int Index, int SequenceIndex, RenderTreeFrame Frame) element, string name)
    {
        var framesRange = MediaTestUtil.CurrentFrames ?? throw new InvalidOperationException("Current frames not set");
        for (var i = element.Index + 1; i < framesRange.Count; i++)
        {
            ref readonly var frame = ref framesRange.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute)
            {
                if (string.Equals(frame.AttributeName, name, StringComparison.Ordinal))
                {
                    return true;
                }
                continue;
            }
            break;
        }
        return false;
    }

    private static string ReadInnerText(ArrayRange<RenderTreeFrame> frames)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text)
            {
                return frame.TextContent;
            }
        }
        return string.Empty;
    }

    private static TestRenderer CreateRenderer(IJSRuntime js)
    {
        var services = new TestServiceProvider();
        services.AddService<IJSRuntime>(js);
        var renderer = new InteractiveTestRenderer(services);
        return renderer;
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

        public int Count(string identifier) => _invocations.Count(i => i.Identifier == identifier);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? Array.Empty<object?>());

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var invocation = new Invocation(identifier, cancellationToken, args ?? Array.Empty<object?>());
            _invocations.Enqueue(invocation);

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
            try
            {
                await Task.Delay(50, token);
            }
            catch
            {
                // ignore cancellation
            }
            object boxed = Result;
            return (TValue)boxed;
        }

        private record struct Invocation(string Identifier, CancellationToken Token, object?[] Args);
    }

    // Utility to capture current frames for attribute search (simplified approach replaced after implementation refinement)
    private static class MediaTestUtil
    {
        [ThreadStatic]
        public static ArrayRange<RenderTreeFrame>? CurrentFrames;
    }
}
