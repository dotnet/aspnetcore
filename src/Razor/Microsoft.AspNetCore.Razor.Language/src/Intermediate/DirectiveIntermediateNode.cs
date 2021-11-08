// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class DirectiveIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public string DirectiveName { get; set; }

    public IEnumerable<DirectiveTokenIntermediateNode> Tokens => Children.OfType<DirectiveTokenIntermediateNode>();

    public DirectiveDescriptor Directive { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitDirective(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(DirectiveName);

        formatter.WriteProperty(nameof(Directive), Directive?.DisplayName);
        formatter.WriteProperty(nameof(DirectiveName), DirectiveName);
    }
}
