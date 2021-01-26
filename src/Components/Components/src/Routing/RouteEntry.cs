// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable disable warnings

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Components.Routing
{
    [DebuggerDisplay("Handler = {Handler}, Template = {Template}")]
    internal class RouteEntry
    {
        public RouteEntry(RouteTemplate template, Type handler, string[] unusedRouteParameterNames)
        {
            Template = template;
            UnusedRouteParameterNames = unusedRouteParameterNames;
            Handler = handler;
        }

        public RouteTemplate Template { get; }

        public string[] UnusedRouteParameterNames { get; }

        public Type Handler { get; }

        internal void Match(RouteContext context)
        {
            var pathIndex = 0;
            var templateIndex = 0;
            Dictionary<string, object> parameters = null;
            // We will iterate over the path segments and the template segments until we have consumed
            // one of them.
            // There are three cases we need to account here for:
            // * Path is shorter than template ->
            //   * This can match only if we have t-p optional parameters at the end.
            // * Path and template have the same number of segments
            //   * This can happen when the catch-all segment matches 1 segment
            //   * This can happen when an optional parameter has been specified.
            //   * This can happen when the route only contains literals and parameters.
            // * Path is longer than template -> This can only match if the parameter has a catch-all at the end.
            //   * We still need to iterate over all the path segments if the catch-all is constrained.
            //   * We still need to iterate over all the template/path segments before the catch-all
            while (pathIndex < context.Segments.Length && templateIndex < Template.Segments.Length)
            {
                var pathSegment = context.Segments[pathIndex];
                var templateSegment = Template.Segments[templateIndex];

                var matches = templateSegment.Match(pathSegment, out var match);
                if (!matches)
                {
                    // A constraint or literal didn't match
                    return;
                }

                if (!templateSegment.IsCatchAll)
                {
                    // We were dealing with a literal or a parameter, so just advance both cursors.
                    pathIndex++;
                    templateIndex++;

                    if (templateSegment.IsParameter)
                    {
                        parameters ??= new(StringComparer.OrdinalIgnoreCase);
                        parameters[templateSegment.Value] = match;
                    }
                }
                else
                {
                    if (templateSegment.Constraints.Length == 0)
                    {

                        // Unconstrained catch all, we can stop early
                        parameters ??= new(StringComparer.OrdinalIgnoreCase);
                        parameters[templateSegment.Value] = string.Join('/', context.Segments, pathIndex, context.Segments.Length - pathIndex);

                        // Mark the remaining segments as consumed.
                        pathIndex = context.Segments.Length;

                        // Catch-alls are always last.
                        templateIndex++;

                        // We are done, so break out of the loop.
                        break;
                    }
                    else
                    {
                        // For constrained catch-alls, we advance the path index but keep the template index on the catch-all.
                        pathIndex++;
                        if (pathIndex == context.Segments.Length)
                        {
                            parameters ??= new(StringComparer.OrdinalIgnoreCase);
                            parameters[templateSegment.Value] = string.Join('/', context.Segments, templateIndex, context.Segments.Length - templateIndex);

                            // This is important to signal that we consumed the entire template.
                            templateIndex++;
                        }
                    }
                }
            }

            var hasRemainingOptionalSegments = templateIndex < Template.Segments.Length &&
                RemainingSegmentsAreOptional(pathIndex, Template.Segments);

            if ((pathIndex == context.Segments.Length && templateIndex == Template.Segments.Length) || hasRemainingOptionalSegments)
            {
                if (hasRemainingOptionalSegments)
                {
                    parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                    AddDefaultValues(parameters, templateIndex, Template.Segments);
                }
                if (UnusedRouteParameterNames?.Length > 0)
                {
                    parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                    for (var i = 0; i < UnusedRouteParameterNames.Length; i++)
                    {
                        parameters[UnusedRouteParameterNames[i]] = null;
                    }
                }
                context.Handler = Handler;
                context.Parameters = parameters;
            }
        }

        private void AddDefaultValues(Dictionary<string, object> parameters, int templateIndex, TemplateSegment[] segments)
        {
            for (var i = templateIndex; i < segments.Length; i++)
            {
                var currentSegment = segments[i];
                parameters[currentSegment.Value] = null;
            }
        }

        private bool RemainingSegmentsAreOptional(int index, TemplateSegment[] segments)
        {
            for (var i = index; index < segments.Length - 1; index++)
            {
                if (!segments[i].IsOptional)
                {
                    return false;
                }
            }

            return segments[^1].IsOptional || segments[^1].IsCatchAll;
        }
    }
}
