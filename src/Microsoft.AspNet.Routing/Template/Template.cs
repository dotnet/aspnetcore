// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
        }

        public List<TemplatePart> Parameters { get; private set; }

        public List<TemplateSegment> Segments { get; private set; }

        private string DebuggerToString()
        {
            return string.Join(SeparatorString, Segments.Select(s => s.DebuggerToString()));
        }
    }
}
