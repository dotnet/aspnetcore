// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebView.Document;

internal class MarkupNode : TestNode
{
    public MarkupNode(string markupContent)
    {
        Content = markupContent;
    }

    public string Content { get; }
}
