// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public sealed class ViewComponentTagHelperIntermediateNode : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = IntermediateNodeCollection.ReadOnly;

    public string ClassName { get; set; }

    public TagHelperDescriptor TagHelper { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        AcceptExtensionNode<ViewComponentTagHelperIntermediateNode>(this, visitor);
    }

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var extension = target.GetExtension<IViewComponentTagHelperTargetExtension>();
        if (extension == null)
        {
            ReportMissingCodeTargetExtension<IViewComponentTagHelperTargetExtension>(context);
            return;
        }

        extension.WriteViewComponentTagHelper(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(ClassName);

        formatter.WriteProperty(nameof(ClassName), ClassName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
