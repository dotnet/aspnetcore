// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class SetTagHelperPropertyIRNode : RazorIRNode
    {
        public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public string TagHelperTypeName { get; set; }

        public string PropertyName { get; set; }

        public string AttributeName { get; set; }

        internal HtmlAttributeValueStyle ValueStyle { get; set; }

        public BoundAttributeDescriptor Descriptor { get; set; }

        public TagHelperBinding Binding { get; set; }

        public bool IsIndexerNameMatch { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitSetTagHelperProperty(this);
        }
    }
}
