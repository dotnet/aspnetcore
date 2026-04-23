// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

/// <summary>
/// An <see cref="IHtmlContent"/> implementation that wraps a <see cref="ReadOnlyMemory{T}"/> of UTF-8 encoded bytes
/// representing pre-encoded HTML literal content from Razor views.
/// </summary>
/// <remarks>
/// This type is used to carry UTF-8 HTML literal bytes through the <see cref="ViewBuffer"/> without converting
/// to a <see cref="string"/> until flush time. When the output encoding is UTF-8, these bytes can potentially be
/// written directly to the response stream without any transcoding.
/// </remarks>
internal sealed class Utf8HtmlLiteralContent : IHtmlContent
{
    /// <summary>
    /// Initializes a new instance of <see cref="Utf8HtmlLiteralContent"/>.
    /// </summary>
    /// <param name="utf8Content">The UTF-8 encoded HTML literal bytes.</param>
    public Utf8HtmlLiteralContent(ReadOnlyMemory<byte> utf8Content)
    {
        Utf8Content = utf8Content;
    }

    /// <summary>
    /// Gets the UTF-8 encoded HTML literal bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Utf8Content { get; }

    /// <inheritdoc />
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // Decode UTF-8 bytes to a string and write directly.
        // The content is pre-encoded HTML literal from the Razor compiler,
        // so no further HTML encoding is needed.
        var value = Encoding.UTF8.GetString(Utf8Content.Span);
        writer.Write(value);
    }
}
