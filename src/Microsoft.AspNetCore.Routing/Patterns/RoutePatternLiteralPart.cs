// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternLiteralPart : RoutePatternPart
    {
        internal RoutePatternLiteralPart(string content)
            : base(RoutePatternPartKind.Literal)
        {
            Debug.Assert(!string.IsNullOrEmpty(content));
            Content = content;
        }

        public string Content { get; }

        internal override string DebuggerToString()
        {
            return Content;
        }
    }
}
