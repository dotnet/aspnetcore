// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class AddTagHelperHtmlAttributeIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadOnlyItemCollection.Empty;

        public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

        public override SourceSpan? Source { get; set; }

        public string Name { get; set; }

        internal HtmlAttributeValueStyle ValueStyle { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitAddTagHelperHtmlAttribute(this);
        }
    }
}
