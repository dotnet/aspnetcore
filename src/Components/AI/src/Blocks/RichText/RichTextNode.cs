// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public abstract class RichTextNode
{
    private List<RichTextNode>? _children;

    public IReadOnlyList<RichTextNode> Children =>
        _children ?? (IReadOnlyList<RichTextNode>)Array.Empty<RichTextNode>();

    public void AddChild(RichTextNode child)
    {
        _children ??= new();
        _children.Add(child);
    }
}
