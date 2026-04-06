// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class InlineCodeNode : RichTextNode
{
    public InlineCodeNode()
    {
    }

    public InlineCodeNode(string code)
    {
        Code = code;
    }

    public string Code { get; set; } = string.Empty;
}
