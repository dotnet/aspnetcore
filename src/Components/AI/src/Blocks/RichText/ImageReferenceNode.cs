// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a reference-style image.
/// </summary>
public class ImageReferenceNode : RichTextNode
{
    /// <summary>
    /// Gets or sets the reference label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alternative text.
    /// </summary>
    public string? Alt { get; set; }

    /// <summary>
    /// Gets or sets the reference syntax.
    /// </summary>
    public ReferenceKind ReferenceKind { get; set; }
}
