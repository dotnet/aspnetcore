// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
#if ROUTING
using Microsoft.AspNetCore.Routing.Template;
#elif DISPATCHER
using Microsoft.AspNetCore.Dispatcher;
#else
#error
#endif

#if ROUTING
namespace Microsoft.AspNetCore.Routing.Tree
#elif DISPATCHER
namespace Microsoft.AspNetCore.Dispatcher
#else
#error
#endif
{
#if ROUTING
    /// <summary>
    /// A candidate route to match incoming URLs in a <see cref="TreeRouter"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public
#elif DISPATCHER
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal
#else
#error
#endif
    class InboundMatch
    {
        /// <summary>
        /// Gets or sets the <see cref="InboundRouteEntry"/>.
        /// </summary>
        public InboundRouteEntry Entry { get; set; }

#if ROUTING
        /// <summary>
        /// Gets or sets the <see cref="TemplateMatcher"/>.
        /// </summary>
        public TemplateMatcher TemplateMatcher { get; set; }

        private string DebuggerToString()
        {
            return TemplateMatcher?.Template?.TemplateText;
        }

#elif DISPATCHER
        /// <summary>
        /// Gets or sets the <see cref="RoutePatternMatcher"/>.
        /// </summary>
        public RoutePatternMatcher RoutePatternMatcher { get; set; }

        private string DebuggerToString()
        {
            return RoutePatternMatcher?.RoutePattern?.RawText;
        }
#else
#error
#endif
}
}
