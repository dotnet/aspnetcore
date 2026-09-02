// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents an image.
/// </summary>
public class ImageNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ImageNode"/>.
    /// </summary>
    public ImageNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageNode"/>.
    /// </summary>
    /// <param name="url">The image URL.</param>
    /// <param name="alt">The alternative text.</param>
    /// <param name="title">The optional title.</param>
    public ImageNode(string url, string? alt = null, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(url);
        Url = url;
        Alt = alt;
        Title = title;
    }

    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alternative text.
    /// </summary>
    public string? Alt { get; set; }

    /// <summary>
    /// Gets or sets the optional title.
    /// </summary>
    public string? Title { get; set; }
}
