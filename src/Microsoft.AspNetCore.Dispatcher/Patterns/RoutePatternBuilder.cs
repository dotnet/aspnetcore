// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    public sealed class RoutePatternBuilder
    {
        private RoutePatternBuilder()
        {
        }

        public IList<RoutePatternPathSegment> PathSegments { get; } = new List<RoutePatternPathSegment>();

        public string Text { get; set; }

        public RoutePatternBuilder AddPathSegment(RoutePatternPart part)
        {
            return AddPathSegment(null, part, Array.Empty<RoutePatternPart>());
        }

        public RoutePatternBuilder AddPathSegment(RoutePatternPart part, params RoutePatternPart[] parts)
        {
            return AddPathSegment(null, part, Array.Empty<RoutePatternPart>());
        }

        public RoutePatternBuilder AddPathSegment(string text, RoutePatternPart part)
        {
            return AddPathSegment(text, part, Array.Empty<RoutePatternPart>());
        }

        public RoutePatternBuilder AddPathSegment(string text, RoutePatternPart part, params RoutePatternPart[] parts)
        {
            var allParts = new RoutePatternPart[1 + parts.Length];
            allParts[0] = part;
            parts.CopyTo(allParts, 1);
            
            var segment = new RoutePatternPathSegment(text, allParts);
            PathSegments.Add(segment);

            return this;
        }

        public RoutePattern Build()
        {
            var parameters = new List<RoutePatternParameter>();
            for (var i = 0; i < PathSegments.Count; i++)
            {
                var segment = PathSegments[i];
                for (var j = 0; j < segment.Parts.Count; j++)
                {
                    var parameter = segment.Parts[j] as RoutePatternParameter;
                    if (parameter != null)
                    {
                        parameters.Add(parameter);
                    }
                }
            }

            return new RoutePattern(Text, parameters.ToArray(), PathSegments.ToArray());
        }

        public static RoutePatternBuilder Create(string text)
        {
            return new RoutePatternBuilder() { Text = text, };
        }
    }
}
