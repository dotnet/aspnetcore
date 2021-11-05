// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentChildContentIntermediateNode : IntermediateNode
{
    public string AttributeName => BoundAttribute?.Name ?? ComponentsApi.RenderTreeBuilder.ChildContent;

    public BoundAttributeDescriptor BoundAttribute { get; set; }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public bool IsParameterized => BoundAttribute?.IsParameterizedChildContentProperty() ?? false;

    public string ParameterName { get; set; }

    public string TypeName { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentChildContent(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(ParameterName), ParameterName);
        formatter.WriteProperty(nameof(TypeName), TypeName);
    }
}
