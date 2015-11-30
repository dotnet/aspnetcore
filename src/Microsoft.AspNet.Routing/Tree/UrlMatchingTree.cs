// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing.Tree
{
    public class UrlMatchingTree
    {
        public UrlMatchingTree(int order)
        {
            Order = order;
        }

        public int Order { get; }

        public UrlMatchingNode Root { get; } = new UrlMatchingNode(length: 0);
    }
}
