// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentTypeArgumentIntermediateNode : IntermediateNode
{
    public ComponentTypeArgumentIntermediateNode(TagHelperPropertyIntermediateNode propertyNode)
    {
        if (propertyNode == null)
        {
            throw new ArgumentNullException(nameof(propertyNode));
        }

        BoundAttribute = propertyNode.BoundAttribute;
        Source = propertyNode.Source;
        TagHelper = propertyNode.TagHelper;

        for (var i = 0; i < propertyNode.Children.Count; i++)
        {
            Children.Add(propertyNode.Children[i]);
        }

        for (var i = 0; i < propertyNode.Diagnostics.Count; i++)
        {
            Diagnostics.Add(propertyNode.Diagnostics[i]);
        }
    }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public BoundAttributeDescriptor BoundAttribute { get; set; }

    public string TypeParameterName => BoundAttribute.Name;

    public TagHelperDescriptor TagHelper { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentTypeArgument(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(TypeParameterName);

        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
