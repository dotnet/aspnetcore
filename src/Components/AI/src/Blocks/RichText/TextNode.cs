// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents plain text.
/// </summary>
public class TextNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="TextNode"/>.
    /// </summary>
    public TextNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TextNode"/>.
    /// </summary>
    /// <param name="text">The text.</param>
    public TextNode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
    }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
