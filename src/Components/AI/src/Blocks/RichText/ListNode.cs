// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class ListNode : RichTextNode
{
    public ListNode()
    {
    }

    public ListNode(bool ordered, int? start = null)
    {
        Ordered = ordered;
        Start = start;
    }

    public bool Ordered { get; set; }

    public int? Start { get; set; }
}
