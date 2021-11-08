// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class RazorSourceChecksumAttributeIntermediateNode : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public byte[] Checksum { get; set; }

    public string ChecksumAlgorithm { get; set; }

    public string Identifier { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        AcceptExtensionNode<RazorSourceChecksumAttributeIntermediateNode>(this, visitor);
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

        var extension = target.GetExtension<IMetadataAttributeTargetExtension>();
        if (extension == null)
        {
            ReportMissingCodeTargetExtension<IMetadataAttributeTargetExtension>(context);
            return;
        }

        extension.WriteRazorSourceChecksumAttribute(context, this);
    }
}
