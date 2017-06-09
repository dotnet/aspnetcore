// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class CreateTagHelperIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadOnlyItemCollection.Empty;

        public override RazorIRNodeCollection Children => ReadOnlyIRNodeCollection.Instance;

        public override SourceSpan? Source { get; set; }

        public string TagHelperTypeName { get; set; }

        public TagHelperDescriptor Descriptor { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitCreateTagHelper(this);
        }
    }
}
