// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class TagHelperPropertyIntermediateNode : IntermediateNode
    {
        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public string AttributeName { get; set; }

        public AttributeStructure AttributeStructure { get; set; }

        public BoundAttributeDescriptor BoundAttribute { get; set; }

        public TagHelperDescriptor TagHelper { get; set; }

        public bool IsIndexerNameMatch { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitTagHelperProperty(this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            formatter.WriteContent(AttributeName);

            formatter.WriteProperty(nameof(AttributeName), AttributeName);
            formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
            formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
            formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
            formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
        }
    }
}
