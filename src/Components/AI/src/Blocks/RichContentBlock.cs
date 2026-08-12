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
    /// Gets the paragraphs of <see cref="RawText"/>, split on blank lines.
    /// </summary>
    public IReadOnlyList<string> Paragraphs { get; internal set; } = Array.Empty<string>();

    /// <summary>
    /// Appends a text fragment to this block.
    /// </summary>
    /// <param name="text">The fragment to append.</param>
    public void AppendText(string text)
    {
        _segments.Add(text);
        _cachedText = null;
    }
}
