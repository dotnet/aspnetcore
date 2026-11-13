// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents an ordered or unordered list.
/// </summary>
public class ListNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListNode"/>.
    /// </summary>
    public ListNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ListNode"/>.
    /// </summary>
    /// <param name="ordered">Whether the list is ordered.</param>
    /// <param name="start">The optional starting number.</param>
    public ListNode(bool ordered, int? start = null)
    {
        Ordered = ordered;
        Start = start;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the list is ordered.
    /// </summary>
    public bool Ordered { get; set; }

    /// <summary>
    /// Gets or sets the starting number of an ordered list.
    /// </summary>
    public int? Start { get; set; }
}
