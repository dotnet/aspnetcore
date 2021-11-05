// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract class IntermediateNodeWalker : IntermediateNodeVisitor
{
    private readonly List<IntermediateNode> _ancestors = new List<IntermediateNode>();

    protected IReadOnlyList<IntermediateNode> Ancestors => _ancestors;

    protected IntermediateNode Parent => _ancestors.Count > 0 ? _ancestors[0] : null;

    public override void VisitDefault(IntermediateNode node)
    {
        var children = node.Children;
        if (node.Children.Count == 0)
        {
            return;
        }

        _ancestors.Insert(0, node);

        try
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = children[i];
                Visit(child);
            }
        }
        finally
        {
            _ancestors.RemoveAt(0);
        }
    }
}
