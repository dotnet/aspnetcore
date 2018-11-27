// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Components.Routing
{
    internal class RouteTemplate
    {
        public static readonly char[] Separators = new[] { '/' };

        public RouteTemplate(string TemplateText, TemplateSegment[] segments)
        {
            this.TemplateText = TemplateText;
            Segments = segments;
        }

        public string TemplateText { get; }

        public TemplateSegment[] Segments { get; }
    }
}
