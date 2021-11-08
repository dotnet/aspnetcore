// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class PropertyDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public IList<string> Modifiers { get; } = new List<string>();

    public string PropertyName { get; set; }

    public string PropertyType { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitPropertyDeclaration(this);
    }
}
