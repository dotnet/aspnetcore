// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    internal class CommentNode : ContainerNode
    {
        public CommentNode(string commentContent)
        {
            CommentContent = commentContent;
        }

        public string CommentContent { get; }
    }
}
