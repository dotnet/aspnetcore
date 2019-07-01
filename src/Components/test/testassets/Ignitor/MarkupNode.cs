// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
