// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a node in structured conversational text.
/// </summary>
/// <remarks>
/// This hierarchy describes presentation semantics and does not prescribe a source format
/// or parser. Applications can map output from Markdig or any other parser into these nodes.
/// The mapper is responsible for interpreting source syntax, resolving references, and
/// deciding which presentation node represents each source construct.
/// </remarks>
public abstract class RichTextNode
{
    private List<RichTextNode>? _children;

    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    public IReadOnlyList<RichTextNode> Children =>
        _children ?? (IReadOnlyList<RichTextNode>)Array.Empty<RichTextNode>();

    /// <summary>
    /// Adds a child node.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(RichTextNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        _children ??= new();
        _children.Add(child);
    }
}
