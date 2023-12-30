// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Html;

/// <summary>
/// An <see cref="IHtmlContentBuilder"/> implementation using an in memory list.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
public class HtmlContentBuilder : IHtmlContentBuilder
{
    /// <summary>
    /// Creates a new <see cref="HtmlContentBuilder"/>.
    /// </summary>
    public HtmlContentBuilder()
        : this(new List<object>())
    {
    }

    /// <summary>
    /// Creates a new <see cref="HtmlContentBuilder"/> with the given initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the backing store.</param>
    public HtmlContentBuilder(int capacity)
        : this(new List<object>(capacity))
    {
    }

    /// <summary>
    /// Gets the number of elements in the <see cref="HtmlContentBuilder"/>.
    /// </summary>
    public int Count => Entries.Count;

    /// <summary>
    /// Creates a new <see cref="HtmlContentBuilder"/> with the given list of entries.
    /// </summary>
    /// <param name="entries">
    /// The list of entries. The <see cref="HtmlContentBuilder"/> will use this list without making a copy.
    /// </param>
    public HtmlContentBuilder(IList<object> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Entries = entries;
    }

    // This is not List<IHtmlContent> because that would lead to wrapping all strings to IHtmlContent
    // which is not space performant.
    //
    // In general unencoded strings are added here. We're optimizing for that case, and allocating
    // a wrapper when encoded strings are used.
    //
    // internal for testing.
    internal IList<object> Entries { get; }

    /// <inheritdoc />
    public IHtmlContentBuilder Append(string? unencoded)
    {
        if (!string.IsNullOrEmpty(unencoded))
        {
            Entries.Add(unencoded);
        }

        return this;
    }

    /// <inheritdoc />
    public IHtmlContentBuilder AppendHtml(IHtmlContent? htmlContent)
    {
        if (htmlContent == null)
        {
            return this;
        }

        Entries.Add(htmlContent);
        return this;
    }

    /// <inheritdoc />
    public IHtmlContentBuilder AppendHtml(string? encoded)
    {
        if (!string.IsNullOrEmpty(encoded))
        {
            Entries.Add(new HtmlString(encoded));
        }

        return this;
    }

    /// <inheritdoc />
    public IHtmlContentBuilder Clear()
    {
        Entries.Clear();
        return this;
    }

    /// <inheritdoc />
    public void CopyTo(IHtmlContentBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var count = Entries.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = Entries[i];

            if (entry is string entryAsString)
            {
                destination.Append(entryAsString);
            }
            else if (entry is IHtmlContentContainer entryAsContainer)
            {
                // Since we're copying, do a deep flatten.
                entryAsContainer.CopyTo(destination);
            }
            else
            {
                // Only string, IHtmlContent values can be added to the buffer.
                destination.AppendHtml((IHtmlContent)entry);
            }
        }
    }

    /// <inheritdoc />
    public void MoveTo(IHtmlContentBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var count = Entries.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = Entries[i];

            if (entry is string entryAsString)
            {
                destination.Append(entryAsString);
            }
            else if (entry is IHtmlContentContainer entryAsContainer)
            {
                // Since we're moving, do a deep flatten.
                entryAsContainer.MoveTo(destination);
            }
            else
            {
                // Only string, IHtmlContent values can be added to the buffer.
                destination.AppendHtml((IHtmlContent)entry);
            }
        }

        Entries.Clear();
    }

    /// <inheritdoc />
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);

        var count = Entries.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = Entries[i];

            var entryAsString = entry as string;
            if (entryAsString != null)
            {
                encoder.Encode(writer, entryAsString);
            }
            else
            {
                // Only string, IHtmlContent values can be added to the buffer.
                ((IHtmlContent)entry).WriteTo(writer, encoder);
            }
        }
    }

    private string DebuggerToString()
    {
        using var writer = new StringWriter();
        WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
