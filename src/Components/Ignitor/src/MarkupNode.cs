// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ignitor
{
    public class MarkupNode : Node
    {
        public MarkupNode(string markupContent)
        {
            MarkupContent = markupContent;
        }

        public string MarkupContent { get; }
    }
}
