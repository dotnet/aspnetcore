// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a footnote reference.
/// </summary>
/// <remarks>
/// A mapper can use this node for a reference such as a Markdig footnote link. The core
/// library does not parse the reference or resolve its label.
/// </remarks>
public class FootnoteReferenceNode : RichTextNode
{
    /// <summary>
    /// Gets or sets the footnote label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
