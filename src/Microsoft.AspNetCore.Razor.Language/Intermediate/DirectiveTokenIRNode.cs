// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class DirectiveTokenIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadOnlyItemCollection.Empty;

        public override RazorIRNodeCollection Children => ReadOnlyIRNodeCollection.Instance;

        public override SourceSpan? Source { get; set; }

        public string Content { get; set; }

        public DirectiveTokenDescriptor Descriptor { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitDirectiveToken(this);
        }
    }
}