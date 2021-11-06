// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperDirectiveAttributeParameterIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public string AttributeName { get; set; }

    public string AttributeNameWithoutParameter { get; set; }

    public string OriginalAttributeName { get; set; }

    public AttributeStructure AttributeStructure { get; set; }

    public BoundAttributeParameterDescriptor BoundAttributeParameter { get; set; }

    public BoundAttributeDescriptor BoundAttribute { get; set; }

    public TagHelperDescriptor TagHelper { get; set; }

    public bool IsIndexerNameMatch { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitTagHelperDirectiveAttributeParameter(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(OriginalAttributeName), OriginalAttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(BoundAttributeParameter), BoundAttributeParameter?.DisplayName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
