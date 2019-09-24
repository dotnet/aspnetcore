// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
