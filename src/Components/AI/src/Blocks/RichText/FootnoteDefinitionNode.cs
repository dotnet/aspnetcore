// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a footnote definition.
/// </summary>
/// <remarks>
/// A mapper can use this node for labeled footnote content, such as content represented by
/// a Markdig footnote container. The core library does not parse or resolve footnote syntax.
/// </remarks>
public class FootnoteDefinitionNode : RichTextNode
{
    /// <summary>
    /// Gets or sets the footnote label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
