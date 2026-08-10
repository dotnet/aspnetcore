// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a footnote reference.
/// </summary>
public class FootnoteReferenceNode : RichTextNode
{
    /// <summary>
    /// Gets or sets the footnote label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
