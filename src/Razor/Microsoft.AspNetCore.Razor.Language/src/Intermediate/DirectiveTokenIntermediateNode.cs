// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class DirectiveTokenIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public string Content { get; set; }

    public DirectiveTokenDescriptor DirectiveToken { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitDirectiveToken(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Content);

        formatter.WriteProperty(nameof(Content), Content);
    }
}
