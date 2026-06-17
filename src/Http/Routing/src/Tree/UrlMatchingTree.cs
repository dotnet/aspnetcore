// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
#if COMPONENTS
using Microsoft.AspNetCore.Routing.Patterns;
#else
using System.Linq;
using Microsoft.AspNetCore.Routing.Template;
#endif

namespace Microsoft.AspNetCore.Routing.Tree;

#if !COMPONENTS
/// <summary>
/// A tree part of a <see cref="TreeRouter"/>.
/// </summary>
public class UrlMatchingTree
#else
internal class UrlMatchingTree
#endif
{
    /// <summary>
    /// Initializes a new instance of <see cref="UrlMatchingTree"/>.
    /// </summary>
    /// <param name="order">The order associated with routes in this <see cref="UrlMatchingTree"/>.</param>
    public UrlMatchingTree(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the order of the routes associated with this <see cref="UrlMatchingTree"/>.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the root of the <see cref="UrlMatchingTree"/>.
    /// </summary>
    public UrlMatchingNode Root { get; } = new UrlMatchingNode(length: 0);

    internal void AddEntry(InboundRouteEntry entry)
    {
        // The url matching tree represents all the routes asociated with a given
        // order. Each node in the tree represents all the different categories
        // a segment can have for which there is a defined inbound route entry.
        // Each node contains a set of Matches that indicate all the routes for which
        // a URL is a potential match. This list contains the routes with the same
        // number of segments and the routes with the same number of segments plus an
        // additional catch all parameter (as it can be empty).
        // For example, for a set of routes like:
        // 'Customer/Index/{id}'
        // '{Controller}/{Action}/{*parameters}'
        //
        // The route tree will look like:
        // Root ->
        //     Literals: Customer ->
        //                   Literals: Index ->
        //                                Parameters: {id}
        //                                                Matches: 'Customer/Index/{id}'
        //     Parameters: {Controller} ->
        //                     Parameters: {Action} ->
        //                                     Matches: '{Controller}/{Action}/{*parameters}'
        //                                     CatchAlls: {*parameters}
        //                                                    Matches: '{Controller}/{Action}/{*parameters}'
        //
        // When the tree router tries to match a route, it iterates the list of url matching trees
        // in ascending order. For each tree it traverses each node starting from the root in the
        // following order: Literals, constrained parameters, parameters, constrained catch all routes, catch alls.
        // When it gets to a node of the same length as the route its trying to match, it simply looks at the list of
        // candidates (which is in precence order) and tries to match the url against it.
        //

        var current = Root;
#if !COMPONENTS
        var matcher = new TemplateMatcher(entry.RouteTemplate, entry.Defaults);
        for (var i = 0; i < entry.RouteTemplate.Segments.Count; i++)
        {
            var segment = entry.RouteTemplate.Segments[i];
#else
        var matcher = new RoutePatternMatcher(entry.RoutePattern, entry.Defaults);
        for (var i = 0; i < entry.RoutePattern.PathSegments.Count; i++)
        {
            var segment = entry.RoutePattern.PathSegments[i];
#endif
            if (!segment.IsSimple)
            {
                // Treat complex segments as a constrained parameter
                if (current.ConstrainedParameters == null)
                {
                    current.ConstrainedParameters = new UrlMatchingNode(length: i + 1);
                }

                current = current.ConstrainedParameters;
                continue;
            }

            Debug.Assert(segment.Parts.Count == 1);
            var part = segment.Parts[0];
#if !COMPONENTS
            if (part.IsLiteral)
            {
                if (!current.Literals.TryGetValue(part.Text, out var next))
                {
                    next = new UrlMatchingNode(length: i + 1);
                    current.Literals.Add(part.Text, next);
                }
#else
            if (part is RoutePatternLiteralPart literalPart)
            {
                if (!current.Literals.TryGetValue(literalPart.Content, out var next))
                {
                    next = new UrlMatchingNode(length: i + 1);
                    current.Literals.Add(literalPart.Content, next);
                }
#endif

                current = next;
                continue;
            }

            // We accept templates that have intermediate optional values, but we ignore
            // those values for route matching. For that reason, we need to add the entry
            // to the list of matches, only if the remaining segments are optional. For example:
            // /{controller}/{action=Index}/{id} will be equivalent to /{controller}/{action}/{id}
            // for the purposes of route matching.
#if !COMPONENTS
            if (part.IsParameter &&
                RemainingSegmentsAreOptional(entry.RouteTemplate.Segments, i))
#else
            if (part.IsParameter &&
                RemainingSegmentsAreOptional(entry.RoutePattern.PathSegments, i))
#endif
            {
                current.Matches.Add(new InboundMatch() { Entry = entry, TemplateMatcher = matcher });
            }

#if !COMPONENTS
            if (part.IsParameter && part.InlineConstraints.Any() && !part.IsCatchAll)
            {
                if (current.ConstrainedParameters == null)
                {
                    current.ConstrainedParameters = new UrlMatchingNode(length: i + 1);
                }

                current = current.ConstrainedParameters;
                continue;
            }

            if (part.IsParameter && !part.IsCatchAll)
            {
                if (current.Parameters == null)
                {
                    current.Parameters = new UrlMatchingNode(length: i + 1);
                }

                current = current.Parameters;
                continue;
            }

            if (part.IsParameter && part.InlineConstraints.Any() && part.IsCatchAll)
            {
                if (current.ConstrainedCatchAlls == null)
                {
                    current.ConstrainedCatchAlls = new UrlMatchingNode(length: i + 1) { IsCatchAll = true };
                }

                current = current.ConstrainedCatchAlls;
                continue;
            }

            if (part.IsParameter && part.IsCatchAll)
            {
                if (current.CatchAlls == null)
                {
                    current.CatchAlls = new UrlMatchingNode(length: i + 1) { IsCatchAll = true };
                }

                current = current.CatchAlls;
                continue;
            }
#else
            if (part is RoutePatternParameterPart parameterPart)
            {
                if (parameterPart.ParameterPolicies.Count > 0 && !parameterPart.IsCatchAll)
                {
                    if (current.ConstrainedParameters == null)
                    {
                        current.ConstrainedParameters = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.ConstrainedParameters;
                    continue;
                }

                if (!parameterPart.IsCatchAll)
                {
                    if (current.Parameters == null)
                    {
                        current.Parameters = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.Parameters;
                    continue;
                }

                if (parameterPart.ParameterPolicies.Count > 0 && parameterPart.IsCatchAll)
                {
                    if (current.ConstrainedCatchAlls == null)
                    {
                        current.ConstrainedCatchAlls = new UrlMatchingNode(length: i + 1) { IsCatchAll = true };
                    }

                    current = current.ConstrainedCatchAlls;
                    continue;
                }

                if (parameterPart.IsCatchAll)
                {
                    if (current.CatchAlls == null)
                    {
                        current.CatchAlls = new UrlMatchingNode(length: i + 1) { IsCatchAll = true };
                    }

                    current = current.CatchAlls;
                    continue;
                }
            }
#endif

            Debug.Fail("We shouldn't get here.");
        }

        current.Matches.Add(new InboundMatch() { Entry = entry, TemplateMatcher = matcher });
        current.Matches.Sort((x, y) =>
        {
            var result = x.Entry.Precedence.CompareTo(y.Entry.Precedence);
#if !COMPONENTS
            return result == 0 ? string.Compare(x.Entry.RouteTemplate.TemplateText, y.Entry.RouteTemplate.TemplateText, StringComparison.Ordinal) : result;
#else
            return result == 0 ? string.Compare(x.Entry.RoutePattern.RawText, y.Entry.RoutePattern.RawText, StringComparison.Ordinal) : result;
#endif
        });
    }

#if !COMPONENTS
    private static bool RemainingSegmentsAreOptional(IList<TemplateSegment> segments, int currentParameterIndex)
#else
    private static bool RemainingSegmentsAreOptional(IReadOnlyList<RoutePatternPathSegment> segments, int currentParameterIndex)
#endif
    {
        for (var i = currentParameterIndex; i < segments.Count; i++)
        {
            if (!segments[i].IsSimple)
            {
                // /{complex}-{segment}
                return false;
            }

            var part = segments[i].Parts[0];
            if (!part.IsParameter)
            {
                // /literal
                return false;
            }

#if !COMPONENTS
            var isOptionlCatchAllOrHasDefaultValue = part.IsOptional ||
                part.IsCatchAll ||
                part.DefaultValue != null;
#else
            var isOptionlCatchAllOrHasDefaultValue = part is RoutePatternParameterPart parameterPart &&
                    (parameterPart.IsOptional ||
                    parameterPart.IsCatchAll ||
                    parameterPart.Default != null);
#endif

            if (!isOptionlCatchAllOrHasDefaultValue)
            {
                // /{parameter}
                return false;
            }
        }

        return true;
    }
}
