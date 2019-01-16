// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// Resprents a literal text part of a route pattern. Instances of <see cref="RoutePatternLiteralPart"/>
    /// are immutable.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternLiteralPart : RoutePatternPart
    {
        internal RoutePatternLiteralPart(string content)
            : base(RoutePatternPartKind.Literal)
        {
            Debug.Assert(!string.IsNullOrEmpty(content));
            Content = content;
        }

        /// <summary>
        /// Gets the text content.
        /// </summary>
        public string Content { get; }

        internal override string DebuggerToString()
        {
            return Content;
        }
    }
}
