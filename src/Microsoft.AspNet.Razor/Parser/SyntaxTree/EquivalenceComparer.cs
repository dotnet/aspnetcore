// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    internal class EquivalenceComparer : IEqualityComparer<SyntaxTreeNode>
    {
        public bool Equals(SyntaxTreeNode x, SyntaxTreeNode y)
        {
            return x.EquivalentTo(y);
        }

        public int GetHashCode(SyntaxTreeNode obj)
        {
            return obj.GetHashCode();
        }
    }
}
