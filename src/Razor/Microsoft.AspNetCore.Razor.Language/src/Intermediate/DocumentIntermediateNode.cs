// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class DocumentIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public string DocumentKind { get; set; }

    public RazorCodeGenerationOptions Options { get; set; }

    public CodeTarget Target { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitDocument(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(DocumentKind);

        formatter.WriteProperty(nameof(DocumentKind), DocumentKind);
    }
}
