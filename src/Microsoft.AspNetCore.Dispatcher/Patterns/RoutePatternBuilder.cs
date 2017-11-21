// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    public sealed class RoutePatternBuilder
    {
        private RoutePatternBuilder()
        {
        }

        public IList<RoutePatternPathSegment> PathSegments { get; } = new List<RoutePatternPathSegment>();

        public string RawText { get; set; }

        public RoutePatternBuilder AddPathSegment(params RoutePatternPart[] parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            if (parts.Length == 0)
            {
                throw new ArgumentException(Resources.RoutePatternBuilder_CollectionCannotBeEmpty, nameof(parts));
            }

            return AddPathSegmentFromText(null, parts);
        }

        public RoutePatternBuilder AddPathSegmentFromText(string text, params RoutePatternPart[] parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            if (parts.Length == 0)
            {
                throw new ArgumentException(Resources.RoutePatternBuilder_CollectionCannotBeEmpty, nameof(parts));
            }

            var segment = new RoutePatternPathSegment(text, parts.ToArray());
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
                    if (segment.Parts[j] is RoutePatternParameter parameter)
                    {
                        parameters.Add(parameter);
                    }
                }
            }

            return new RoutePattern(RawText, parameters.ToArray(), PathSegments.ToArray());
        }

        public static RoutePatternBuilder Create(string text)
        {
            return new RoutePatternBuilder() { RawText = text, };
        }
    }
}
