// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class ImageNode : RichTextNode
{
    public ImageNode()
    {
    }

    public ImageNode(string url, string? alt = null, string? title = null)
    {
        Url = url;
        Alt = alt;
        Title = title;
    }

    public string Url { get; set; } = string.Empty;

    public string? Alt { get; set; }

    public string? Title { get; set; }
}
