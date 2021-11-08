// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class FieldDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public IList<string> Modifiers { get; } = new List<string>();

    public IList<string> SuppressWarnings { get; } = new List<string>();

    public string FieldName { get; set; }

    public string FieldType { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitFieldDeclaration(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(FieldName);

        formatter.WriteProperty(nameof(FieldName), FieldName);
        formatter.WriteProperty(nameof(FieldType), FieldType);
        formatter.WriteProperty(nameof(Modifiers), string.Join(" ", Modifiers));
    }
}
