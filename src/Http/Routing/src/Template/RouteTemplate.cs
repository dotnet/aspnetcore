// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template;

/// <summary>
/// Represents the template for a route.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
public class RouteTemplate
{
    private const string SeparatorString = "/";

    /// <summary>
    /// Constructs a new <see cref="RouteTemplate"/> instance given <paramref name="other"/>.
    /// </summary>
    /// <param name="other">A <see cref="RoutePattern"/> instance.</param>
    public RouteTemplate(RoutePattern other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // RequiredValues will be ignored. RouteTemplate doesn't support them.

        TemplateText = other.RawText;

        Segments = new List<TemplateSegment>(other.PathSegments.Count);
        foreach (var p in other.PathSegments)
        {
            Segments.Add(new TemplateSegment(p));
        }

        Parameters = new List<TemplatePart>();
        for (var i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            for (var j = 0; j < segment.Parts.Count; j++)
            {
                var part = segment.Parts[j];
                if (part.IsParameter)
                {
                    Parameters.Add(part);
                }
            }
        }
    }

    /// <summary>
    /// Constructs a a new <see cref="RouteTemplate" /> instance given the <paramref name="template"/> string
    /// and a list of <paramref name="segments"/>. Computes the parameters in the route template.
    /// </summary>
    /// <param name="template">A string representation of the route template.</param>
    /// <param name="segments">A list of <see cref="TemplateSegment"/>.</param>
    public RouteTemplate(string template, List<TemplateSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        TemplateText = template;

        Segments = segments;

        Parameters = new List<TemplatePart>();
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = Segments[i];
            for (var j = 0; j < segment.Parts.Count; j++)
            {
                var part = segment.Parts[j];
                if (part.IsParameter)
                {
                    Parameters.Add(part);
                }
            }
        }
    }

    /// <summary>
    /// Gets the string representation of the route template.
    /// </summary>
    public string? TemplateText { get; }

    /// <summary>
    /// Gets the list of <see cref="TemplatePart"/> that represent that parameters defined in the route template.
    /// </summary>
    public IList<TemplatePart> Parameters { get; }

    /// <summary>
    /// Gets the list of <see cref="TemplateSegment"/> that compromise the route template.
    /// </summary>
    public IList<TemplateSegment> Segments { get; }

    /// <summary>
    /// Gets the <see cref="TemplateSegment"/> at a given index.
    /// </summary>
    /// <param name="index">The index of the element to retrieve.</param>
    /// <returns>A <see cref="TemplateSegment"/> instance.</returns>
    public TemplateSegment? GetSegment(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return index >= Segments.Count ? null : Segments[index];
    }

    private string DebuggerToString()
    {
        return string.Join(SeparatorString, Segments.Select(s => s.DebuggerToString()));
    }

    /// <summary>
    /// Gets the parameter matching the given name.
    /// </summary>
    /// <param name="name">The name of the parameter to match.</param>
    /// <returns>The matching parameter or <c>null</c> if no parameter matches the given name.</returns>
    public TemplatePart? GetParameter(string name)
    {
        for (var i = 0; i < Parameters.Count; i++)
        {
            var parameter = Parameters[i];
            if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts the <see cref="RouteTemplate"/> to the equivalent
    /// <see cref="RoutePattern"/>
    /// </summary>
    /// <returns>A <see cref="RoutePattern"/>.</returns>
    public RoutePattern ToRoutePattern()
    {
        var segments = Segments.Select(s => s.ToRoutePatternPathSegment());
        return RoutePatternFactory.Pattern(TemplateText, segments);
    }
}
