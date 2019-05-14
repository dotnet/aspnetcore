// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class HtmlAttributeIntermediateNode : IntermediateNode
    {
        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public string AttributeName { get; set; }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitHtmlAttribute(this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            formatter.WriteContent(AttributeName);

            formatter.WriteProperty(nameof(AttributeName), AttributeName);
            formatter.WriteProperty(nameof(Prefix), Prefix);
            formatter.WriteProperty(nameof(Suffix), Suffix);
        }
    }
}
