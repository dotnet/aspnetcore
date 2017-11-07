// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// A candidate endpoint to match incoming URLs in a <c>TreeMatcher</c>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class InboundMatch
    {
        /// <summary>
        /// Gets or sets the <see cref="InboundRouteEntry"/>.
        /// </summary>
        public InboundRouteEntry Entry { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RoutePatternMatcher"/>.
        /// </summary>
        public RoutePatternMatcher RoutePatternMatcher { get; set; }

        private string DebuggerToString()
        {
            return RoutePatternMatcher?.RoutePattern?.RawText;
        }
    }
}
