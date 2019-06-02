// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    internal class TextNode : Node
    {
        private readonly string _text;

        public TextNode(string text)
        {
            _text = text;
        }
    }
}
