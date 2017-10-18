// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternPathSegment
    {
        internal RoutePatternPathSegment(string rawText, RoutePatternPart[] parts)
        {
            RawText = rawText;
            Parts = parts;
        }

        public bool IsSimple => Parts.Count == 1;

        public IReadOnlyList<RoutePatternPart> Parts { get; }

        public string RawText { get; set; }

        internal string DebuggerToString()
        {
            return RawText ?? DebuggerToString(Parts);
        }

        internal static string DebuggerToString(IReadOnlyList<RoutePatternPart> parts)
        {
            return string.Join(string.Empty, parts.Select(p => p.DebuggerToString()));
        }
    }
}
