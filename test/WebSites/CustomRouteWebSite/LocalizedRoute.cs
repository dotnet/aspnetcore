// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;

namespace CustomRouteWebSite
{
    public class LocalizedRoute : IRouter
    {
        private readonly IRouter _next;

        private readonly Dictionary<string, string> _users = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Javier", "es-ES" },
            { "Doug", "en-CA" },
        };

        public LocalizedRoute(IRouter next)
        {
            _next = next;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            // We just want to act as a pass-through for link generation
            return _next.GetVirtualPath(context);
        }

        public async Task RouteAsync(RouteContext context)
        {
            // Saving and restoring the original route data ensures that any values we
            // add won't 'leak' if action selection doesn't match.
            var oldRouteData = context.RouteData;

            // For diagnostics and link-generation purposes, routing should include
            // a list of IRoute instances that lead to the ultimate destination.
            // It's the responsibility of each IRouter to add the 'next' before 
            // calling it.
            var newRouteData = new RouteData(oldRouteData);
            newRouteData.Routers.Add(_next);

            var locale = GetLocale(context.HttpContext) ?? "en-US";
            newRouteData.Values.Add("locale", locale);

            try
            {
                context.RouteData = newRouteData;
                await _next.RouteAsync(context);
            }
            finally
            {
                if (!context.IsHandled)
                {
                    context.RouteData = oldRouteData;
                }
            }
        }

        private string GetLocale(HttpContext context)
        {
            string locale;
            _users.TryGetValue(context.Request.Headers["User"], out locale);
            return locale;
        }
    }
}