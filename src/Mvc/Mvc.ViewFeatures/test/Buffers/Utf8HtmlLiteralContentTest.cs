// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

public class Utf8HtmlLiteralContentTest
{
    [Fact]
    public void WriteTo_WritesDecodedUtf8Content()
    {
        var utf8Bytes = Encoding.UTF8.GetBytes("<h1>Hello World</h1>");
        var content = new Utf8HtmlLiteralContent(utf8Bytes);
        var writer = new StringWriter();

        content.WriteTo(writer, HtmlEncoder.Default);

        Assert.Equal("<h1>Hello World</h1>", writer.ToString());
    }

    [Fact]
    public void WriteTo_WithEmptyContent_WritesNothing()
    {
        var content = new Utf8HtmlLiteralContent(ReadOnlyMemory<byte>.Empty);
        var writer = new StringWriter();

        content.WriteTo(writer, HtmlEncoder.Default);

        Assert.Empty(writer.ToString());
    }

    [Fact]
    public void WriteTo_DoesNotHtmlEncode()
    {
        var utf8Bytes = Encoding.UTF8.GetBytes("<div class=\"test\">&amp;</div>");
        var content = new Utf8HtmlLiteralContent(utf8Bytes);
        var writer = new StringWriter();

        // Using HtmlTestEncoder which wraps output in HtmlEncode[[...]] if encoding is applied
        content.WriteTo(writer, new HtmlTestEncoder());

        // Content should be written as-is, not encoded
        Assert.Equal("<div class=\"test\">&amp;</div>", writer.ToString());
    }

    [Fact]
    public void Utf8Content_ReturnsOriginalMemory()
    {
        var utf8Bytes = Encoding.UTF8.GetBytes("test content");
        var memory = new ReadOnlyMemory<byte>(utf8Bytes);
        var content = new Utf8HtmlLiteralContent(memory);

        Assert.True(memory.Span.SequenceEqual(content.Utf8Content.Span));
    }

    [Fact]
    public void WriteTo_WithMultiByteUtf8Characters_WritesCorrectly()
    {
        var text = "<p>Héllo Wörld — 日本語</p>";
        var utf8Bytes = Encoding.UTF8.GetBytes(text);
        var content = new Utf8HtmlLiteralContent(utf8Bytes);
        var writer = new StringWriter();

        content.WriteTo(writer, HtmlEncoder.Default);

        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void WriteTo_ThrowsForNullWriter()
    {
        var content = new Utf8HtmlLiteralContent(new byte[] { 0x48 });

        Assert.Throws<ArgumentNullException>("writer", () => content.WriteTo(null!, HtmlEncoder.Default));
    }
}
