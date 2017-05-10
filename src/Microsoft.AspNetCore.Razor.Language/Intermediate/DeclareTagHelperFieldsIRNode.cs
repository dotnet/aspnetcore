// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class DeclareTagHelperFieldsIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadonlyItemCollection.Empty;

        public override IList<RazorIRNode> Children { get; } = EmptyArray;

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public ISet<string> UsedTagHelperTypeNames { get; set; } = new HashSet<string>(StringComparer.Ordinal);

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitDeclareTagHelperFields(this);
        }
    }
}
