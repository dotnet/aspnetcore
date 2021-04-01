// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Components.LegacyRouteMatching
{
    [DebuggerDisplay("{TemplateText}")]
    internal class LegacyRouteTemplate
    {
        public LegacyRouteTemplate(string templateText, LegacyTemplateSegment[] segments)
        {
            TemplateText = templateText;
            Segments = segments;
            OptionalSegmentsCount = segments.Count(template => template.IsOptional);
            ContainsCatchAllSegment = segments.Any(template => template.IsCatchAll);
        }

        public string TemplateText { get; }

        public LegacyTemplateSegment[] Segments { get; }

        public int OptionalSegmentsCount { get; }

        public bool ContainsCatchAllSegment { get; }
    }
}
