// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class TemplateSegment
    {
        private readonly List<TemplatePart> _parts = new List<TemplatePart>();

        public List<TemplatePart> Parts
        {
            get { return _parts; }
        }

        internal string DebuggerToString()
        {
            return string.Join(string.Empty, Parts.Select(p => p.DebuggerToString()));
        }
    }
}
