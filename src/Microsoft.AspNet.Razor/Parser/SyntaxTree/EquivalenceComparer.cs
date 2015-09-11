// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    internal class EquivalenceComparer : IEqualityComparer<SyntaxTreeNode>
    {
        public bool Equals(SyntaxTreeNode nodeX, SyntaxTreeNode nodeY)
        {
            if (nodeX == nodeY)
            {
                return true;
            }

            return nodeX != null && nodeX.EquivalentTo(nodeY);
        }

        public int GetHashCode(SyntaxTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return node.GetEquivalenceHash();
        }
    }
}
