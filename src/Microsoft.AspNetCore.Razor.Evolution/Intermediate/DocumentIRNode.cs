// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public sealed class DocumentIRNode : RazorIRNode
    {
        public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

        public string DocumentKind { get; set; }

        public override RazorIRNode Parent { get; set; }

        internal override MappingLocation SourceRange { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitDocument(this);
        }

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            return visitor.VisitDocument(this);
        }
    }
}
