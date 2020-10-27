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
            string? catchAllValue = null;

            // If this template contains a catch-all parameter, we can concatenate the pathSegments
            // at and beyond the catch-all segment's position. For example:
            // Template:        /foo/bar/{*catchAll}
            // PathSegments:    /foo/bar/one/two/three
            if (Template.ContainsCatchAllSegment && context.Segments.Length >= Template.Segments.Length)
            {
                catchAllValue = string.Join('/', context.Segments[Range.StartAt(Template.Segments.Length - 1)]);
            }
            // If there are no optional segments on the route and the length of the route
            // and the template do not match, then there is no chance of this matching and
            // we can bail early.
            else if (Template.OptionalSegmentsCount == 0 && Template.Segments.Length != context.Segments.Length)
            {
                return;
            }

            // Parameters will be lazily initialized.
            Dictionary<string, object> parameters = null;
            var numMatchingSegments = 0;
            for (var i = 0; i < Template.Segments.Length; i++)
            {
                var segment = Template.Segments[i];

                if (segment.IsCatchAll)
                {
                    numMatchingSegments += 1;
                    parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                    parameters[segment.Value] = catchAllValue;
                    break;
                }

                // If the template contains more segments than the path, then
                // we may need to break out of this for-loop. This can happen
                // in one of two cases:
                //
                // (1) If we are comparing a literal route with a literal template
                // and the route is shorter than the template.
                // (2) If we are comparing a template where the last value is an optional
                // parameter that the route does not provide.
                if (i >= context.Segments.Length)
                {
                    // If we are under condition (1) above then we can stop evaluating
                    // matches on the rest of this template.
                    if (!segment.IsParameter && !segment.IsOptional)
                    {
                        break;
                    }
                }

                string pathSegment = null;
                if (i < context.Segments.Length)
                {
                    pathSegment = context.Segments[i];
                }

                if (!segment.Match(pathSegment, out var matchedParameterValue))
                {
                    return;
                }
                else
                {
                    numMatchingSegments++;
                    if (segment.IsParameter)
                    {
                        parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                        parameters[segment.Value] = matchedParameterValue;
                    }
                }
            }

            // In addition to extracting parameter values from the URL, each route entry
            // also knows which other parameters should be supplied with null values. These
            // are parameters supplied by other route entries matching the same handler.
            if (!Template.ContainsCatchAllSegment && UnusedRouteParameterNames.Length > 0)
            {
                parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                for (var i = 0; i < UnusedRouteParameterNames.Length; i++)
                {
                    parameters[UnusedRouteParameterNames[i]] = null;
                }
            }

            // We track the number of segments in the template that matched
            // against this particular route then only select the route that
            // matches the most number of segments on the route that was passed.
            // This check is an exactness check that favors the more precise of
            // two templates in the event that the following route table exists.
            //  Route 1: /{anythingGoes}
            //  Route 2: /users/{id:int}
            // And the provided route is `/users/1`. We want to choose Route 2
            // over Route 1.
            // Furthermore, literal routes are preferred over parameterized routes.
            // If the two routes below are registered in the route table.
            // Route 1: /users/1
            // Route 2: /users/{id:int}
            // And the provided route is `/users/1`. We want to choose Route 1 over
            // Route 2.
            var allRouteSegmentsMatch = numMatchingSegments >= context.Segments.Length;
            // Checking that all route segments have been matches does not suffice if we are
            // comparing literal templates with literal routes. For example, the template
            // `/this/is/a/template` and the route `/this/`. In that case, we want to ensure
            // that all non-optional segments have matched as well.
            var allNonOptionalSegmentsMatch = numMatchingSegments >= (Template.Segments.Length - Template.OptionalSegmentsCount);
            if (Template.ContainsCatchAllSegment || (allRouteSegmentsMatch && allNonOptionalSegmentsMatch))
            {
                context.Parameters = parameters;
                context.Handler = Handler;
            }
        }
    }
}
