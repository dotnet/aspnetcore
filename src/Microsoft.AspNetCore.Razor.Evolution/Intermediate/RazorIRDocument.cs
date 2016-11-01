// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public sealed class RazorIRDocument : RazorIRNode
    {
        // Only allow creation of documents through the builder API because
        // they can't be nested.
        internal RazorIRDocument()
        {
            Children = new List<RazorIRNode>();
        }

        public override IList<RazorIRNode> Children { get; }

        public override RazorIRNode Parent { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitDocument(this);
        }

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            return visitor.VisitDocument(this);
        }
    }
}
