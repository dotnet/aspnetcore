// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class DirectiveIRNode : RazorIRNode
    {
        private ItemCollection _annotations;

        public override ItemCollection Annotations
        {
            get
            {
                if (_annotations == null)
                {
                    _annotations = new DefaultItemCollection();
                }

                return _annotations;
            }
        }

        public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public string Name { get; set; }

        public IEnumerable<DirectiveTokenIRNode> Tokens => Children.OfType<DirectiveTokenIRNode>();

        public DirectiveDescriptor Descriptor { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitDirective(this);
        }
    }
}