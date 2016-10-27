// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public sealed class IRDocument : IRNode
    {
        // Only allow creation of documents through the builder API because
        // they can't be nested.
        internal IRDocument()
        {
            Children = new List<IRNode>();
        }

        public override IList<IRNode> Children { get; }

        public override IRNode Parent { get; set; }

        public override void Accept(IRNodeVisitor visitor)
        {
            visitor.VisitDocument(this);
        }

        public override TResult Accept<TResult>(IRNodeVisitor<TResult> visitor)
        {
            return visitor.VisitDocument(this);
        }
    }
}
