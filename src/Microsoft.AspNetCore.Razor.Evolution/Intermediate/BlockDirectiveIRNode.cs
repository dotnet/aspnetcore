// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class BlockDirectiveIRNode : RazorIRNode
    {
        public string Name { get; set; }

        public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

        public override RazorIRNode Parent { get; set; }

        internal override SourceLocation SourceLocation { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitBlockDirective(this);
        }

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            return visitor.VisitBlockDirective(this);
        }
    }
}
