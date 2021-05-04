// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Routing
{
    [DebuggerDisplay("{TemplateText}")]
    internal class RouteTemplate
    {
        public RouteTemplate(string templateText, TemplateSegment[] segments)
        {
            TemplateText = templateText;
            Segments = segments;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment.IsOptional)
                {
                    OptionalSegmentsCount++;
                }
                if (segment.IsCatchAll)
                {
                    ContainsCatchAllSegment = true;
                }
            }
        }

        public string TemplateText { get; }

        public TemplateSegment[] Segments { get; }

        public int OptionalSegmentsCount { get; }

        public bool ContainsCatchAllSegment { get; }
    }
}
