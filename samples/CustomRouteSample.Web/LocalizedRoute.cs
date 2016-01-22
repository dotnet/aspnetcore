// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CustomRouteSample.Web
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
            context.RouteData.Routers.Add(_next);

            var locale = GetLocale(context.HttpContext) ?? "en-US";
            context.RouteData.Values.Add("locale", locale);

            await _next.RouteAsync(context);
        }

        private string GetLocale(HttpContext context)
        {
            string locale;
            _users.TryGetValue(context.Request.Headers["User"], out locale);
            return locale;
        }
    }
}