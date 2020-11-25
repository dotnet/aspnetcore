// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


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
