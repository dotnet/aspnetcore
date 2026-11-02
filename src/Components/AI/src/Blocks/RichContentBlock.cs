// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// A block of conversational text. Text arrives in fragments while the model streams,
/// so the block accumulates the fragments and exposes both the raw text and the
/// paragraphs derived from it.
/// </summary>
public class RichContentBlock : ContentBlock
{
    private readonly List<string> _segments = new();
    private string? _cachedText;

    /// <summary>
    /// Gets the concatenation of every text fragment received so far.
    /// </summary>
    public string RawText => _cachedText ??= string.Concat(_segments);

    /// <summary>
    /// Gets the structured representation of <see cref="RawText"/>.
    /// </summary>
    public IReadOnlyList<RichTextNode> Content { get; internal set; } = Array.Empty<RichTextNode>();

    /// <summary>
    /// Appends a text fragment to this block.
    /// </summary>
    /// <param name="text">The fragment to append.</param>
    public void AppendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        _segments.Add(text);
        _cachedText = null;
    }

    internal void ReplaceContent(string text, IReadOnlyList<RichTextNode> content)
    {
        _segments.Clear();
        _segments.Add(text);
        _cachedText = text;
        Content = content;
    }
}
