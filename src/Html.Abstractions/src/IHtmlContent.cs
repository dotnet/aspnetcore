// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Html;

/// <summary>
/// HTML content which can be written to a TextWriter.
/// </summary>
public interface IHtmlContent
{
    /// <summary>
    /// Writes the content by encoding it with the specified <paramref name="encoder"/>
    /// to the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to which the content is written.</param>
    /// <param name="encoder">The <see cref="HtmlEncoder"/> which encodes the content to be written.</param>
    void WriteTo(TextWriter writer, HtmlEncoder encoder);
}
