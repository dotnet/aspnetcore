// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class Template
    {
        private const string SeparatorString = "/";

        private readonly TemplateMatcher _matcher;
        private readonly TemplateBinder _binder;

        public Template(List<TemplateSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException("segments");
            }

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

            _matcher = new TemplateMatcher(this);
            _binder = new TemplateBinder(this);
        }

        public List<TemplatePart> Parameters { get; private set; }

        public List<TemplateSegment> Segments { get; private set; }

        public IDictionary<string, object> Match(string requestPath, IDictionary<string, object> defaults)
        {
            return _matcher.Match(requestPath, defaults);
        }

        private string DebuggerToString()
        {
            return string.Join(SeparatorString, Segments.Select(s => s.DebuggerToString()));
        }
    }
}
