// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class RazorIRNodeWalker : RazorIRNodeVisitor
    {
        private readonly Stack<RazorIRNode> _ancestors = new Stack<RazorIRNode>();

        protected IEnumerable<RazorIRNode> Ancestors => _ancestors;

        protected RazorIRNode Parent => _ancestors.Count > 0 ? _ancestors.Peek() : null;

        public override void VisitDefault(RazorIRNode node)
        {
            var children = node.Children;
            if (node.Children.Count == 0)
            {
                return;
            }

            _ancestors.Push(node);

            try
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = children[i];
                    Visit(child);
                }
            }
            finally
            {
                _ancestors.Pop();
            }
        }
    }
}
