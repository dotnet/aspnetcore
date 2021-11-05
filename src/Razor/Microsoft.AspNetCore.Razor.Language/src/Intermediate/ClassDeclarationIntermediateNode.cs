// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ClassDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public IList<string> Modifiers { get; } = new List<string>();

    public string ClassName { get; set; }

    public string BaseType { get; set; }

    public IList<string> Interfaces { get; set; } = new List<string>();

    public IList<TypeParameter> TypeParameters { get; set; } = new List<TypeParameter>();

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitClassDeclaration(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(ClassName);

        formatter.WriteProperty(nameof(BaseType), BaseType);
        formatter.WriteProperty(nameof(ClassName), ClassName);
        formatter.WriteProperty(nameof(Interfaces), string.Join(", ", Interfaces));
        formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(TypeParameters), string.Join(", ", TypeParameters.Select(t => t.ParameterName)));
    }
}
