// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class HtmlAttributeValueIRNode : RazorIRNode
    {
        public override IList<RazorIRNode> Children { get; } = EmptyArray;

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public string Prefix { get; set; }

        public string Content { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitHtmlAttributeValue(this);
        }
    }
}
