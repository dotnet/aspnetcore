// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    internal class DeclarePreallocatedTagHelperAttributeIRNode : RazorIRNode
    {
        public override IList<RazorIRNode> Children { get; } = EmptyArray;

        public override RazorIRNode Parent { get; set; }

        internal override MappingLocation SourceRange { get; set; }

        public string VariableName { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public HtmlAttributeValueStyle ValueStyle { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitDeclarePreallocatedTagHelperAttribute(this);
        }

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            return visitor.VisitDeclarePreallocatedTagHelperAttribute(this);
        }
    }
}
