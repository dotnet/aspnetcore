// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

/// <summary>
/// <para>
/// A <see cref="TextWriter"/> that is backed by a unbuffered writer (over the Response stream) and/or a
/// <see cref="ViewBuffer"/>
/// </para>
/// <para>
/// When <c>Flush</c> or <c>FlushAsync</c> is invoked, the writer copies all content from the buffer to
/// the writer and switches to writing to the unbuffered writer for all further write operations.
/// </para>
/// </summary>
internal class ViewBufferTextWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly HtmlEncoder _htmlEncoder;

    /// <summary>
    /// Creates a new instance of <see cref="ViewBufferTextWriter"/>.
    /// </summary>
    /// <param name="buffer">The <see cref="ViewBuffer"/> for buffered output.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding"/>.</param>
    public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        Buffer = buffer;
        Encoding = encoding;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ViewBufferTextWriter"/>.
    /// </summary>
    /// <param name="buffer">The <see cref="ViewBuffer"/> for buffered output.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding"/>.</param>
    /// <param name="htmlEncoder">The HTML encoder.</param>
    /// <param name="inner">
    /// The inner <see cref="TextWriter"/> to write output to when this instance is no longer buffering.
    /// </param>
    public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding, HtmlEncoder htmlEncoder, TextWriter inner)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (htmlEncoder == null)
        {
            throw new ArgumentNullException(nameof(htmlEncoder));
        }

        if (inner == null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        Buffer = buffer;
        Encoding = encoding;
        _htmlEncoder = htmlEncoder;
        _inner = inner;
    }

    /// <inheritdoc />
    public override Encoding Encoding { get; }

    /// <summary>
    /// Gets the <see cref="ViewBuffer"/>.
    /// </summary>
    public ViewBuffer Buffer { get; }

    /// <summary>
    /// Gets a value that indicates if <see cref="Flush"/> or <see cref="FlushAsync" /> was invoked.
    /// </summary>
    public bool Flushed { get; private set; }

    /// <inheritdoc />
    public override void Write(char value)
    {
        Buffer.AppendHtml(value.ToString());
    }

    /// <inheritdoc />
    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (buffer.Length - index < count)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }

        Buffer.AppendHtml(new string(buffer, index, count));
    }

    /// <inheritdoc />
    public override void Write(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        Buffer.AppendHtml(value);
    }

    /// <inheritdoc />
    public override void Write(object value)
    {
        if (value == null)
        {
            return;
        }

        if (value is IHtmlContentContainer container)
        {
            Write(container);
        }
        else if (value is IHtmlContent htmlContent)
        {
            Write(htmlContent);
        }
        else
        {
            Write(value.ToString());
        }
    }

    /// <summary>
    /// Writes an <see cref="IHtmlContent"/> value.
    /// </summary>
    /// <param name="value">The <see cref="IHtmlContent"/> value.</param>
    public void Write(IHtmlContent value)
    {
        if (value == null)
        {
            return;
        }

        Buffer.AppendHtml(value);
    }

    /// <summary>
    /// Writes an <see cref="IHtmlContentContainer"/> value.
    /// </summary>
    /// <param name="value">The <see cref="IHtmlContentContainer"/> value.</param>
    public void Write(IHtmlContentContainer value)
    {
        if (value == null)
        {
            return;
        }

        value.MoveTo(Buffer);
    }

    /// <inheritdoc />
    public override void WriteLine(object value)
    {
        if (value == null)
        {
            return;
        }

        if (value is IHtmlContentContainer container)
        {
            Write(container);
            Write(NewLine);
        }
        else if (value is IHtmlContent htmlContent)
        {
            Write(htmlContent);
            Write(NewLine);
        }
        else
        {
            Write(value.ToString());
            Write(NewLine);
        }
    }

    /// <inheritdoc />
    public override Task WriteAsync(char value)
    {
        Buffer.AppendHtml(value.ToString());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (count < 0 || (buffer.Length - index < count))
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Buffer.AppendHtml(new string(buffer, index, count));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task WriteAsync(string value)
    {
        Buffer.AppendHtml(value);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override void WriteLine()
    {
        Buffer.AppendHtml(NewLine);
    }

    /// <inheritdoc />
    public override void WriteLine(string value)
    {
        Buffer.AppendHtml(value);
        Buffer.AppendHtml(NewLine);
    }

    /// <inheritdoc />
    public override Task WriteLineAsync(char value)
    {
        Buffer.AppendHtml(value.ToString());
        Buffer.AppendHtml(NewLine);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task WriteLineAsync(char[] value, int start, int offset)
    {
        Buffer.AppendHtml(new string(value, start, offset));
        Buffer.AppendHtml(NewLine);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task WriteLineAsync(string value)
    {
        Buffer.AppendHtml(value);
        Buffer.AppendHtml(NewLine);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task WriteLineAsync()
    {
        Buffer.AppendHtml(NewLine);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Copies the buffered content to the unbuffered writer and invokes flush on it.
    /// </summary>
    public override void Flush()
    {
        if (_inner == null || _inner is ViewBufferTextWriter)
        {
            return;
        }

        Flushed = true;

        Buffer.WriteTo(_inner, _htmlEncoder);
        Buffer.Clear();

        _inner.Flush();
    }

    /// <summary>
    /// Copies the buffered content to the unbuffered writer and invokes flush on it.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous copy and flush operations.</returns>
    public override async Task FlushAsync()
    {
        if (_inner == null || _inner is ViewBufferTextWriter)
        {
            return;
        }

        Flushed = true;

        await Buffer.WriteToAsync(_inner, _htmlEncoder);
        Buffer.Clear();

        await _inner.FlushAsync();
    }
}
