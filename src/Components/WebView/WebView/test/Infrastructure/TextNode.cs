// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebView.Document;

internal class TextNode : TestNode
{
    public TextNode(string textContent)
    {
        Text = textContent;
    }

    public string Text { get; set; }
}
