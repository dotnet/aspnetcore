// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Tree;

/// <summary>
/// A node in a <see cref="UrlMatchingTree"/>.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
#if !COMPONENTS
public class UrlMatchingNode
#else
internal class UrlMatchingNode
#endif
{

    /// <summary>
    /// Initializes a new instance of <see cref="UrlMatchingNode"/>.
    /// </summary>
    /// <param name="length">The length of the path to this node in the <see cref="UrlMatchingTree"/>.</param>
    public UrlMatchingNode(int length)
    {
        Depth = length;

        Matches = new List<InboundMatch>();
        Literals = new Dictionary<string, UrlMatchingNode>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the length of the path to this node in the <see cref="UrlMatchingTree"/>.
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this node represents a catch all segment.
    /// </summary>
    public bool IsCatchAll { get; set; }

    /// <summary>
    /// Gets the list of matching route entries associated with this node.
    /// </summary>
    /// <remarks>
    /// These entries are sorted by precedence then template.
    /// </remarks>
    public List<InboundMatch> Matches { get; }

    /// <summary>
    /// Gets the literal segments following this segment.
    /// </summary>
    public Dictionary<string, UrlMatchingNode> Literals { get; }

    /// <summary>
    /// Gets or sets the <see cref="UrlMatchingNode"/> representing
    /// parameter segments with constraints following this segment in the <see cref="TreeRouter"/>.
    /// </summary>
    public UrlMatchingNode ConstrainedParameters { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="UrlMatchingNode"/> representing
    /// parameter segments following this segment in the <see cref="TreeRouter"/>.
    /// </summary>
    public UrlMatchingNode Parameters { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="UrlMatchingNode"/> representing
    /// catch all parameter segments with constraints following this segment in the <see cref="TreeRouter"/>.
    /// </summary>
    public UrlMatchingNode ConstrainedCatchAlls { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="UrlMatchingNode"/> representing
    /// catch all parameter segments following this segment in the <see cref="TreeRouter"/>.
    /// </summary>
    public UrlMatchingNode CatchAlls { get; set; }

    private string DebuggerToString()
    {
#if !COMPONENTS
        return $"Length: {Depth}, Matches: {string.Join(" | ", Matches?.Select(m => $"({m.TemplateMatcher.Template.TemplateText})"))}";
#else
        return $"Length: {Depth}, Matches: {string.Join(" | ", Matches?.Select(m => $"({m.TemplateMatcher.RoutePattern.RawText})"))}";
#endif
    }
}
