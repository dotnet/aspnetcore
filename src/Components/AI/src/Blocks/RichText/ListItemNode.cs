// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a list item.
/// </summary>
public class ListItemNode : RichTextNode
{
    /// <summary>
    /// Gets or sets the checked state when the item is a task.
    /// </summary>
    public bool? Checked { get; set; }
}
