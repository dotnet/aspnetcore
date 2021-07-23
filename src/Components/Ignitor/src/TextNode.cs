// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ignitor
{
    public class TextNode : Node
    {
        public TextNode(string text)
        {
            TextContent = text;
        }

        public string TextContent { get; set; }
    }
}
