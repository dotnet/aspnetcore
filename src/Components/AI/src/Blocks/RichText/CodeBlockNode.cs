// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class CodeBlockNode : RichTextNode
{
    public CodeBlockNode()
    {
    }

    public CodeBlockNode(string code, string? language = null)
    {
        Code = code;
        Language = language;
    }

    public string? Language { get; set; }

    public string Code { get; set; } = string.Empty;
}
