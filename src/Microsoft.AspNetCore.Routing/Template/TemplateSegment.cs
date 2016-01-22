// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class TemplateSegment
    {
        public bool IsSimple => Parts.Count == 1;

        public List<TemplatePart> Parts { get; } = new List<TemplatePart>();

        internal string DebuggerToString()
        {
            return string.Join(string.Empty, Parts.Select(p => p.DebuggerToString()));
        }
    }
}
