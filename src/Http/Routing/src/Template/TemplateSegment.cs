// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template;

/// <summary>
/// Represents a segment of a route template.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
public class TemplateSegment
{
    /// <summary>
    /// Constructs a new <see cref="TemplateSegment"/> instance.
    /// </summary>
    public TemplateSegment()
    {
        Parts = new List<TemplatePart>();
    }

    /// <summary>
    /// Constructs a new <see cref="TemplateSegment"/> instance given another <see cref="RoutePatternPathSegment"/>.
    /// </summary>
    /// <param name="other">A <see cref="RoutePatternPathSegment"/> instance.</param>
    public TemplateSegment(RoutePatternPathSegment other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var partCount = other.Parts.Count;
        Parts = new List<TemplatePart>(partCount);
        for (var i = 0; i < partCount; i++)
        {
            Parts.Add(new TemplatePart(other.Parts[i]));
        }
    }

    /// <summary>
    /// <see langword="true"/> if the segment contains a single entry.
    /// </summary>
    public bool IsSimple => Parts.Count == 1;

    /// <summary>
    /// Gets the list of individual parts in the template segment.
    /// </summary>
    public List<TemplatePart> Parts { get; }

    internal string DebuggerToString()
    {
        return string.Join(string.Empty, Parts.Select(p => p.DebuggerToString()));
    }

    /// <summary>
    /// Returns a <see cref="RoutePatternPathSegment"/> for the template segment.
    /// </summary>
    /// <returns>A <see cref="RoutePatternPathSegment"/> instance.</returns>
    public RoutePatternPathSegment ToRoutePatternPathSegment()
    {
        var parts = Parts.Select(p => p.ToRoutePatternPart());
        return RoutePatternFactory.Segment(parts);
    }
}
