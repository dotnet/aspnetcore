// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class RouteTemplate
    {
        private const string SeparatorString = "/";

        public RouteTemplate(string template, List<TemplateSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            TemplateText = template;

            Segments = segments;

            Parameters = new List<TemplatePart>();
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = Segments[i];
                for (var j = 0; j < segment.Parts.Count; j++)
                {
                    var part = segment.Parts[j];
                    if (part.IsParameter)
                    {
                        Parameters.Add(part);
                    }
                }
            }
        }

        public string TemplateText { get; }

        public IList<TemplatePart> Parameters { get; }

        public IList<TemplateSegment> Segments { get; }

        public TemplateSegment GetSegment(int index)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            return index >= Segments.Count ? null : Segments[index];
        }

        private string DebuggerToString()
        {
            return string.Join(SeparatorString, Segments.Select(s => s.DebuggerToString()));
        }
    }
}
