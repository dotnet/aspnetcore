// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class DirectiveTokenIRNode : RazorIRNode
    {
        public override IList<RazorIRNode> Children { get; } = EmptyArray;

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public string Content { get; set; }

        public DirectiveTokenDescriptor Descriptor { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitDirectiveToken(this);
        }
    }
}