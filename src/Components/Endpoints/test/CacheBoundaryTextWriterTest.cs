// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public void Write_WithoutStartCapture_DoesNotCaptureSegments()
    {
        var writer = CreateWriter();

        writer.Write("not captured");
        var segments = writer.StopCapture();

        Assert.Equal(0, segments.Count);
    }

    [Fact]
    public void StartCapture_ThenWrite_CapturesHtml()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.Write("captured");
        var segments = writer.StopCapture();

        Assert.Equal(1, segments.Count);
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
    public void WriteChar_DuringCapture_IsCaptured()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.Write('a');
        writer.Write('b');
        var segments = writer.StopCapture();

        Assert.Equal(1, segments.Count);
    }

    [Fact]
    public void PauseCapture_FlushesBufferAsHtmlSegment()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.Write("first");
        writer.PauseCapture();
        writer.Write("gap");
        writer.StartCapture();
        writer.Write("second");
        var segments = writer.StopCapture();

        // "first" flushed by PauseCapture, "second" flushed by StopCapture
        Assert.Equal(2, segments.Count);
    }

    [Fact]
    public void PauseCapture_WithEmptyBuffer_DoesNotAddSegment()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.PauseCapture();
        var segments = writer.StopCapture();

        Assert.Equal(0, segments.Count);
    }

    [Fact]
    public void CreateHole_AddsHoleSegment()
    {
        var writer = CreateWriter();

        writer.StartCapture();
        writer.Write("<html>");
        writer.PauseCapture();
        writer.CreateHole(typeof(FakeHoleComponent));
        writer.StartCapture();
        writer.Write("</html>");
        var segments = writer.StopCapture();

        // html + hole + html = 3 segments
        Assert.Equal(3, segments.Count);
    }

    [Fact]
    public void Encoding_MatchesInnerWriter()
    {
        var inner = new StringWriter();
        var writer = CreateWriter(inner);

        Assert.Equal(inner.Encoding, writer.Encoding);
    }

    private static CacheBoundaryTextWriter CreateWriter(TextWriter inner = null)
    {
        return new CacheBoundaryTextWriter(inner ?? new StringWriter(), CacheBoundaryVaryBy.None);
    }

    private class FakeHoleComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
