// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

public class ViewBufferValueTest
{
    [Fact]
    public void Utf8Value_RoundTripsWithoutBoxing()
    {
        var utf8Bytes = "<h1>Hello World</h1>"u8.ToArray();
        var value = new ViewBufferValue(utf8Bytes);

        Assert.True(value.IsUtf8Value);
        Assert.True(utf8Bytes.AsSpan().SequenceEqual(value.Utf8Value.Span));
    }

    [Fact]
    public void Value_WithUtf8Value_ReturnsBoxedReadOnlyMemory()
    {
        var utf8Bytes = "<h1>Hello World</h1>"u8.ToArray();
        var value = new ViewBufferValue(utf8Bytes);

        var boxedValue = Assert.IsType<ReadOnlyMemory<byte>>(value.Value);
        Assert.True(utf8Bytes.AsSpan().SequenceEqual(boxedValue.Span));
    }

    [Fact]
    public void Value_WithStringValue_ReturnsString()
    {
        var value = new ViewBufferValue("Hello World");

        Assert.False(value.IsUtf8Value);
        Assert.Equal("Hello World", Assert.IsType<string>(value.Value));
        Assert.True(value.Utf8Value.IsEmpty);
    }

    [Fact]
    public void Value_WithHtmlContent_ReturnsHtmlContent()
    {
        var htmlContent = new HtmlString("<p>Hello World</p>");
        var value = new ViewBufferValue(htmlContent);

        Assert.False(value.IsUtf8Value);
        Assert.Same(htmlContent, Assert.IsAssignableFrom<IHtmlContent>(value.Value));
        Assert.True(value.Utf8Value.IsEmpty);
    }
}
