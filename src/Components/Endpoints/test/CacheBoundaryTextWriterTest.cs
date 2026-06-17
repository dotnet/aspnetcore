// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryTextWriterTest
{
    [Fact]
    public void GetJson_HoleWithRenderFragmentParameter_Throws()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestRenderFragmentHole>(7);
            builder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(0, "inner")));
            builder.CloseComponent();
        };

        var capture = new RenderFragmentCapture(fragment);
        using var captureBuilder = new RenderTreeBuilder();
        capture.Invoke(captureBuilder);
        InvokeChildCaptures(capture);

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None, capture);
        writer.StartCapture();
        writer.CreateHole(typeof(TestRenderFragmentHole), sequence: 7, componentKey: null, renderModeName: null);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.GetJson(NullLogger.Instance));
        Assert.Contains("RenderFragment parameter", ex.Message);
    }

    [Fact]
    public void GetJson_HoleWithoutRenderFragmentParameter_DoesNotThrow()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestRenderFragmentHole>(7);
            builder.AddComponentParameter(8, "Title", "hello");
            builder.CloseComponent();
        };

        var capture = new RenderFragmentCapture(fragment);
        using var captureBuilder = new RenderTreeBuilder();
        capture.Invoke(captureBuilder);

        var writer = new CacheBoundaryTextWriter(new StringWriter(), CacheBoundaryVaryBy.None, capture);
        writer.StartCapture();
        writer.CreateHole(typeof(TestRenderFragmentHole), sequence: 7, componentKey: null, renderModeName: null);

        var json = writer.GetJson(NullLogger.Instance);
        Assert.Contains(nameof(TestRenderFragmentHole), json);
    }

    [CacheBoundaryPolicy]
    private sealed class TestRenderFragmentHole : IComponent
    {
        [Parameter] public string? Title { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private static void InvokeChildCaptures(RenderFragmentCapture capture)
    {
        foreach (var child in capture.ChildCaptures.Values)
        {
            using var builder = new RenderTreeBuilder();
            child.Invoke(builder);
            InvokeChildCaptures(child);
        }
    }
}
