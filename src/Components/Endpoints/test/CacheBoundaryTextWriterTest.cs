// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryTextWriterTest
{
    [Fact]
    public void CreateHole_HoleWithRenderFragmentParameter_Throws()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentHole>(7);
            builder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(0, "inner")));
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            writer.CreateHole(typeof(TestRenderFragmentHole), renderMode: null, capture, NullLogger.Instance));
        Assert.Contains("RenderFragment parameter", ex.Message);
    }

    [Fact]
    public void CreateHole_HoleWithGenericRenderFragment_Throws()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentHole>(7);
            builder.AddAttribute(8, "ItemTemplate", (RenderFragment<string>)(item => b => b.AddContent(0, item)));
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();

        var ex = Assert.Throws<InvalidOperationException>(() => writer.CreateHole(typeof(TestRenderFragmentHole), renderMode: null, capture, NullLogger.Instance));
        Assert.Contains("RenderFragment", ex.Message);
    }

    [Fact]
    public void CreateHole_HoleWithoutRenderFragmentParameter_SerializesNode()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentHole>(7);
            builder.AddComponentParameter(8, "Title", "hello");
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();
        writer.CreateHole(typeof(TestRenderFragmentHole), renderMode: null, capture, NullLogger.Instance);
        writer.StopCapture();

        var json = writer.GetJson();
        Assert.Contains(nameof(TestRenderFragmentHole), json);
        Assert.Contains("hello", json);
    }

    [Fact]
    public void GetJson_InterleavesMarkupAndHolesInRenderOrder()
    {
        var capture = new RenderFragmentCapture(CaptureFramesFor(builder =>
        {
            builder.OpenComponent<TestRenderFragmentHole>(7);
            builder.AddComponentParameter(8, "Title", "hole-value");
            builder.CloseComponent();
        }));

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None);
        writer.StartCapture();
        writer.Write("<p>before</p>");
        writer.PauseCapture();
        writer.CreateHole(typeof(TestRenderFragmentHole), renderMode: null, capture, NullLogger.Instance);
        writer.StartCapture();
        writer.Write("<p>after</p>");
        writer.StopCapture();

        var json = writer.GetJson();
        var beforeIndex = json.IndexOf("before", StringComparison.Ordinal);
        var holeIndex = json.IndexOf("hole-value", StringComparison.Ordinal);
        var afterIndex = json.IndexOf("after", StringComparison.Ordinal);
        Assert.True(beforeIndex >= 0 && holeIndex > beforeIndex && afterIndex > holeIndex);
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

    [CacheBoundaryPolicy]
    private sealed class TestRenderFragmentHole : IComponent
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
