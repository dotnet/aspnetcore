// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class HtmlNode : RichTextNode
{
    public HtmlNode()
    {
    }

    public HtmlNode(string value)
    {
        Value = value;
    }

    public string Value { get; set; } = string.Empty;
}
