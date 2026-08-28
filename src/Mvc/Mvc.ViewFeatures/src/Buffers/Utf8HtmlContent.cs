// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal sealed class Utf8HtmlContent : IHtmlAsyncContent
{
    private readonly ReadOnlyMemory<byte> _utf8Content;

    public Utf8HtmlContent(ReadOnlySpan<byte> utf8Content)
    {
        _utf8Content = utf8Content.ToArray();
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);

        if (writer.Encoding is UTF8Encoding && writer is IUtf8TextWriter utf8Writer)
        {
            utf8Writer.WriteUtf8(_utf8Content.Span);
            return;
        }

        writer.Write(Encoding.UTF8.GetString(_utf8Content.Span));
    }

    public ValueTask WriteToAsync(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (writer.Encoding is UTF8Encoding && writer is IUtf8TextWriter utf8Writer)
        {
            return new ValueTask(utf8Writer.WriteUtf8Async(_utf8Content));
        }

        return new ValueTask(writer.WriteAsync(Encoding.UTF8.GetString(_utf8Content.Span)));
    }
}
