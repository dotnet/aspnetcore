// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentParameterDataIntermediateNode : IntermediateNode
{
    private readonly List<BoundAttributeDescriptor> _parameterData = new();

    public IReadOnlyList<BoundAttributeDescriptor> ParameterData => _parameterData;

    public override IntermediateNodeCollection Children { get; } = IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor is null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentParameterData(this);
    }

    public void AddParameterData(BoundAttributeDescriptor descriptor)
    {
        _parameterData.Add(descriptor);
    }
}
