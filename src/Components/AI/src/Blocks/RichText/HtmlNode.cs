// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents HTML source. The default renderer displays the source as text.
/// </summary>
public class HtmlNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="HtmlNode"/>.
    /// </summary>
    public HtmlNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HtmlNode"/>.
    /// </summary>
    /// <param name="value">The HTML source.</param>
    public HtmlNode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>
    /// Gets or sets the HTML source.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
