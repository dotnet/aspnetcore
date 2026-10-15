// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a hyperlink.
/// </summary>
public class LinkNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="LinkNode"/>.
    /// </summary>
    public LinkNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LinkNode"/>.
    /// </summary>
    /// <param name="url">The link URL.</param>
    /// <param name="title">The optional title.</param>
    public LinkNode(string url, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(url);
        Url = url;
        Title = title;
    }

    /// <summary>
    /// Gets or sets the link URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional title.
    /// </summary>
    public string? Title { get; set; }
}
