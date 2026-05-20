// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryTextWriterTest
{
    [Fact]
    public void Write_AlwaysForwardsToInner()
    {
        var inner = new StringWriter();
        var writer = CreateWriter(inner);

        writer.Write("hello");

        Assert.Equal("hello", inner.ToString());
    }

    [Fact]
    public void WriteChar_AlwaysForwardsToInner()
    {
        var inner = new StringWriter();
        var writer = CreateWriter(inner);

        writer.Write('x');

        Assert.Equal("x", inner.ToString());
    }

    [Fact]
    public void Encoding_MatchesInnerWriter()
    {
        var inner = new StringWriter();
        var writer = CreateWriter(inner);

        Assert.Equal(inner.Encoding, writer.Encoding);
    }

    [Fact]
    public void GetJson_WithoutCapture_ReturnsValidJson()
    {
        var writer = CreateWriter();

        writer.Write("not captured");
        writer.StopCapture();

        var json = writer.GetJson(NullLogger.Instance);

        Assert.False(string.IsNullOrEmpty(json));
        Assert.StartsWith("{", json);
    }

    [Fact]
    public void GetJson_OnlyMarkup_IncludesCapturedHtml()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.Write("<p>hello</p>");
        writer.StopCapture();

        var json = writer.GetJson(NullLogger.Instance);

        Assert.Contains("hello", json);
    }

    [Fact]
    public void GetJson_MarkupAroundPause_EmitsTwoMarkupNodes()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.Write("first");
        writer.PauseCapture();
        writer.Write("gap");
        writer.StartCapture();
        writer.Write("second");
        writer.StopCapture();

        var json = writer.GetJson(NullLogger.Instance);

        Assert.Contains("first", json);
        Assert.Contains("second", json);
        Assert.DoesNotContain("gap", json);
    }

    [Fact]
    public void GetJson_HoleWithoutCapture_Throws()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.PauseCapture();
        writer.CreateHole(typeof(ComponentBase), sequence: 0, componentKey: null, renderModeName: null);
        writer.StopCapture();

        var ex = Assert.Throws<InvalidOperationException>(() => writer.GetJson(NullLogger.Instance));
        Assert.Contains("ChildContent capture", ex.Message);
    }

    [Fact]
    public void Write_DuringPausedCapture_ForwardsToInner_ButDoesNotBuffer()
    {
        var inner = new StringWriter();
        var writer = CreateWriter(inner);

        writer.StartCapture();
        writer.Write("captured");
        writer.PauseCapture();
        writer.Write("not-captured");
        writer.StartCapture();
        writer.Write("captured-again");
        writer.StopCapture();

        // Inner writer receives everything
        Assert.Equal("capturednot-capturedcaptured-again", inner.ToString());

        // JSON only has the captured parts
        var json = writer.GetJson(NullLogger.Instance);
        Assert.Contains("captured", json);
        Assert.Contains("captured-again", json);
        Assert.DoesNotContain("not-captured", json);
    }

    [Fact]
    public void IsCapturing_ReflectsCurrentState()
    {
        var writer = CreateWriter();

        Assert.False(writer.IsCapturing);

        writer.StartCapture();
        Assert.True(writer.IsCapturing);

        writer.PauseCapture();
        Assert.False(writer.IsCapturing);

        writer.StartCapture();
        Assert.True(writer.IsCapturing);

        writer.StopCapture();
        Assert.False(writer.IsCapturing);
    }

    [Fact]
    public void GetJson_EmptyCapture_ReturnsValidJsonWithNoMarkup()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        // Write nothing
        writer.StopCapture();

        var json = writer.GetJson(NullLogger.Instance);

        Assert.False(string.IsNullOrEmpty(json));
        Assert.StartsWith("{", json);
    }

    private static CacheBoundaryTextWriter CreateWriter(TextWriter inner = null)
    {
        return new CacheBoundaryTextWriter(inner ?? new StringWriter(), CacheBoundaryVaryBy.None, capture: null);
    }
}
