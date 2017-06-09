// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class HtmlAttributeIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadOnlyItemCollection.Empty;

        public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

        public override SourceSpan? Source { get; set; }

        public string Name { get; set; }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitHtmlAttribute(this);
        }
    }
}
