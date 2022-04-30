// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Routing;

[DebuggerDisplay("{TemplateText}")]
internal sealed class RouteTemplate
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
