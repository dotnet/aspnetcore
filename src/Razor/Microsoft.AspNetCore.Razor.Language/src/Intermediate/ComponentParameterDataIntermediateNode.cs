// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentParameterDataIntermediateNode : IntermediateNode
{
    private readonly List<BoundAttributeDescriptor> _parameterData = new();

    public IReadOnlyList<BoundAttributeDescriptor> ParameterData => _parameterData;

    public string ComponentFullTypeName { get; }

    public override IntermediateNodeCollection Children { get; } = IntermediateNodeCollection.ReadOnly;

    public ComponentParameterDataIntermediateNode(string componentFullTypeName)
    {
        ComponentFullTypeName = componentFullTypeName;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor is null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentParameterData(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteProperty(nameof(ParameterData), FormatParameterData());
    }

    public void AddParameterData(BoundAttributeDescriptor descriptor)
    {
        _parameterData.Add(descriptor);
    }

    internal string FormatParameterData()
    {
        return string.Join(", ", _parameterData.Select(bad => bad.Name));
    }
}
