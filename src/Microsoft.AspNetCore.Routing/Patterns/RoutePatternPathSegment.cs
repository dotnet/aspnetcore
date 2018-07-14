// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternPathSegment
    {
        internal RoutePatternPathSegment(RoutePatternPart[] parts)
        {
            Parts = parts;
        }

        public bool IsSimple => Parts.Count == 1;

        public IReadOnlyList<RoutePatternPart> Parts { get; }

        internal string DebuggerToString()
        {
            return DebuggerToString(Parts);
        }

        internal static string DebuggerToString(IReadOnlyList<RoutePatternPart> parts)
        {
            return string.Join(string.Empty, parts.Select(p => p.DebuggerToString()));
        }
    }
}
