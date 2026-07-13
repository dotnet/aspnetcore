// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryTextWriterTest
{
    [Fact]
    public void CreateLiveCachedComponent_LiveCachedComponentWithRenderFragmentParameter_Throws()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentLiveCachedComponent>(7);
            builder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(0, "inner")));
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            writer.CreateLiveCachedComponent(typeof(TestRenderFragmentLiveCachedComponent), renderMode: null, capture, NullLogger.Instance));
        Assert.Contains("RenderFragment parameter", ex.Message);
    }

    [Fact]
    public void CreateLiveCachedComponent_LiveCachedComponentWithGenericRenderFragment_Throws()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentLiveCachedComponent>(7);
            builder.AddAttribute(8, "ItemTemplate", (RenderFragment<string>)(item => b => b.AddContent(0, item)));
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();

        var ex = Assert.Throws<InvalidOperationException>(() => writer.CreateLiveCachedComponent(typeof(TestRenderFragmentLiveCachedComponent), renderMode: null, capture, NullLogger.Instance));
        Assert.Contains("RenderFragment", ex.Message);
    }

    [Fact]
    public void CreateLiveCachedComponent_LiveCachedComponentWithoutRenderFragmentParameter_SerializesNode()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentLiveCachedComponent>(7);
            builder.AddComponentParameter(8, "Title", "hello");
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();
        writer.CreateLiveCachedComponent(typeof(TestRenderFragmentLiveCachedComponent), renderMode: null, capture, NullLogger.Instance);
        writer.StopCapture();

        var fragment = writer.GetSerializedRenderFragment();
        var json = JsonSerializer.Serialize(fragment, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Contains(nameof(TestRenderFragmentLiveCachedComponent), json);
        Assert.Contains("hello", json);
    }

    [Fact]
    public void GetSerializedRenderFragment_InterleavesMarkupAndLiveCachedComponentsInRenderOrder()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentLiveCachedComponent>(7);
            builder.AddComponentParameter(8, "Title", "live-cached-value");
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();
        writer.Write("<p>before</p>");
        writer.PauseCapture();
        writer.CreateLiveCachedComponent(typeof(TestRenderFragmentLiveCachedComponent), renderMode: null, capture, NullLogger.Instance);
        writer.StartCapture();
        writer.Write("<p>after</p>");
        writer.StopCapture();

        var fragment = writer.GetSerializedRenderFragment();
        var json = JsonSerializer.Serialize(fragment, ServerComponentSerializationSettings.JsonSerializationOptions);
        var beforeIndex = json.IndexOf("before", StringComparison.Ordinal);
        var liveCachedComponentIndex = json.IndexOf("live-cached-value", StringComparison.Ordinal);
        var afterIndex = json.IndexOf("after", StringComparison.Ordinal);
        Assert.True(beforeIndex >= 0 && liveCachedComponentIndex > beforeIndex && afterIndex > liveCachedComponentIndex);
    }

    [Fact]
    public void ThrowIfNestedInsideCapturingBoundary_CapturingWriter_Throws()
    {
        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CacheBoundaryService.ThrowIfNestedInsideCapturingBoundary(writer));
        Assert.Contains("cannot be nested inside another CacheBoundary", ex.Message);
    }

    [Fact]
    public void ThrowIfNestedInsideCapturingBoundary_NonCapturingWriter_DoesNotThrow()
    {
        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();
        writer.StopCapture();

        CacheBoundaryService.ThrowIfNestedInsideCapturingBoundary(writer);
    }

    [Fact]
    public void ThrowIfNestedInsideCapturingBoundary_PlainTextWriter_DoesNotThrow()
    {
        CacheBoundaryService.ThrowIfNestedInsideCapturingBoundary(new StringWriter());
    }

    private static RenderTreeFrame[] CaptureFramesFor(RenderFragment fragment)
    {
        using var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();
        var slice = new RenderTreeFrame[frames.Count];
        Array.Copy(frames.Array, 0, slice, 0, frames.Count);
        return slice;
    }

    [CacheBoundaryLiveComponent]
    private sealed class TestRenderFragmentLiveCachedComponent : IComponent
    {
        [Parameter] public string? Title { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }

        [Parameter] public RenderFragment<string>? ItemTemplate { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
