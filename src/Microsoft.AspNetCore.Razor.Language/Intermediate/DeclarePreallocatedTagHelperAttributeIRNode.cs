// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class DeclarePreallocatedTagHelperAttributeIRNode : RazorIRNode
    {
        public override IList<RazorIRNode> Children { get; } = EmptyArray;

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public string VariableName { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public HtmlAttributeValueStyle ValueStyle { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitDeclarePreallocatedTagHelperAttribute(this);
        }
    }
}
