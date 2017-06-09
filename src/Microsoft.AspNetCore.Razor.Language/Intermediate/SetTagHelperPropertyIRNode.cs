// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class SetTagHelperPropertyIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadOnlyItemCollection.Empty;

        public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

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
