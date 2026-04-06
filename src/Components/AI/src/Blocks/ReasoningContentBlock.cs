// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.AI;

public class ReasoningContentBlock : ContentBlock
{
    private readonly StringBuilder _builder = new();

    public string Text => _builder.ToString();

    public string? ProtectedData { get; set; }

    public bool IsEncrypted => ProtectedData is not null && _builder.Length == 0;

    public void AppendText(string text)
    {
        _builder.Append(text);
    }
}
