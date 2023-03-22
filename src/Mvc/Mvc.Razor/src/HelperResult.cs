// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Represents a deferred write operation in a <see cref="RazorPage"/>.
/// </summary>
public class HelperResult : IHtmlContent
{
    private readonly string _content;

    /// <summary>
    /// Creates a new instance of <see cref="HelperResult"/>.
    /// </summary>
    /// <param name="asyncAction">The asynchronous delegate to invoke when
    /// <see cref="WriteTo(TextWriter, HtmlEncoder)"/> is called.</param>
    /// <remarks>Calls to <see cref="WriteTo(TextWriter, HtmlEncoder)"/> result in a blocking invocation of
    /// <paramref name="asyncAction"/>.</remarks>
    public HelperResult(Func<TextWriter, Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);

        // Hopefully people don't really do async work in the callback, but previously we were just blocking in WriteTo instead.
        using var stringWriter = new StringWriter();
        asyncAction(stringWriter).GetAwaiter().GetResult();
        _content = stringWriter.ToString();
    }

    /// <summary>
    /// Gets the asynchronous delegate to invoke when <see cref="WriteTo(TextWriter, HtmlEncoder)"/> is called.
    /// </summary>
    public Func<TextWriter, Task> WriteAction =>
        writer =>
        {
            writer.Write(_content);
            return Task.CompletedTask;
        };

    /// <summary>
    /// Method invoked to produce content from the <see cref="HelperResult"/>.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
    /// <param name="encoder">The <see cref="HtmlEncoder"/> to encode the content.</param>
    public virtual void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);

        writer.Write(_content);
    }
}
