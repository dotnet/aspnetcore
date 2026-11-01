// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a link or image reference definition.
/// </summary>
public class DefinitionNode : RichTextNode
{
    /// <summary>
    /// Gets or sets the reference label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional title.
    /// </summary>
    public string? Title { get; set; }
}
