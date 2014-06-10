// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// An <see cref="IRouter"/> implementation for attribute routing.
    /// </summary>
    public class AttributeRoute : IRouter
    {
        private readonly IRouter _next;
        private readonly TemplateRoute[] _routes;

        /// <summary>
        /// Creates a new <see cref="AttributeRoute"/>.
        /// </summary>
        /// <param name="next">The next router. Invoked when a route entry matches.</param>
        /// <param name="entries">The set of route entries.</param>
        public AttributeRoute([NotNull] IRouter next, [NotNull] IEnumerable<AttributeRouteEntry> entries)
        {
            _next = next;

            // FOR RIGHT NOW - this is just an array of regular template routes. We'll follow up by implementing
            // a good data-structure here.
            _routes = entries.OrderBy(e => e.Precedence).Select(e => e.Route).ToArray();
        }

        /// <inheritdoc />
        public async Task RouteAsync([NotNull] RouteContext context)
        {
            foreach (var route in _routes)
            {
                await route.RouteAsync(context);
                if (context.IsHandled)
                {
                    return;
                }
            }
        }

        /// <inheritdoc />
        public string GetVirtualPath([NotNull] VirtualPathContext context)
        {
            // Not implemented right now, but we don't want to throw here and block other routes from generating
            // a link.
            return null;
        }
    }
}