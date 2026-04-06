// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class LinkNode : RichTextNode
{
    public LinkNode()
    {
    }

    public LinkNode(string url, string? title = null)
    {
        Url = url;
        Title = title;
    }

    public string Url { get; set; } = string.Empty;

    public string? Title { get; set; }
}
