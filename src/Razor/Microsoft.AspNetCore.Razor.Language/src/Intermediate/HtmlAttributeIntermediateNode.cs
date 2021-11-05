// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class HtmlAttributeIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public CSharpExpressionIntermediateNode AttributeNameExpression { get; set; }

    public string AttributeName { get; set; }

    public string Prefix { get; set; }

    public string Suffix { get; set; }

    public string EventUpdatesAttributeName { get; set; }

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
        formatter.WriteProperty(nameof(AttributeNameExpression), string.Join(string.Empty, AttributeNameExpression?.FindDescendantNodes<IntermediateToken>().Select(n => n.Content) ?? Array.Empty<string>()));
        formatter.WriteProperty(nameof(Prefix), Prefix);
        formatter.WriteProperty(nameof(Suffix), Suffix);
        formatter.WriteProperty(nameof(EventUpdatesAttributeName), EventUpdatesAttributeName);
    }
}
