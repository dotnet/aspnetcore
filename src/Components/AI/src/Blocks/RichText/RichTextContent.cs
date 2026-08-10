// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a complete snapshot of structured text in a streaming chat response.
/// </summary>
/// <remarks>
/// Providers can emit a new snapshot as text streams. The block mapping pipeline replaces
/// the previous snapshot without exposing a partially mutated tree to renderers.
/// </remarks>
public class RichTextContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of <see cref="RichTextContent"/>.
    /// </summary>
    /// <param name="text">The plain-text representation of the content.</param>
    /// <param name="nodes">The structured nodes that represent <paramref name="text"/>.</param>
    public RichTextContent(string text, IReadOnlyList<RichTextNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(nodes);

        Text = text;
        Nodes = [.. nodes];
    }

    /// <summary>
    /// Gets the plain-text representation of the content.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the structured nodes that represent <see cref="Text"/>.
    /// </summary>
    public IReadOnlyList<RichTextNode> Nodes { get; }
}
