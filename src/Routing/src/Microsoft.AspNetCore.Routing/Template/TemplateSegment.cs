// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class TemplateSegment
    {
        public TemplateSegment()
        {
            Parts = new List<TemplatePart>();
        }

        public TemplateSegment(RoutePatternPathSegment other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var partCount = other.Parts.Count;
            Parts = new List<TemplatePart>(partCount);
            for (var i = 0; i < partCount; i++)
            {
                Parts.Add(new TemplatePart(other.Parts[i]));
            }
        }

        public bool IsSimple => Parts.Count == 1;

        public List<TemplatePart> Parts { get; }

        internal string DebuggerToString()
        {
            return string.Join(string.Empty, Parts.Select(p => p.DebuggerToString()));
        }

        public RoutePatternPathSegment ToRoutePatternPathSegment()
        {
            var parts = Parts.Select(p => p.ToRoutePatternPart());
            return RoutePatternFactory.Segment(parts);
        }
    }
}
