// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebView.Document
{
    internal class TextNode : TestNode
    {
        public TextNode(string textContent)
        {
            Text = textContent;
        }

        public string Text { get; set; }
    }
}
