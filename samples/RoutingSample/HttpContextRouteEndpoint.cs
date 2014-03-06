﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;

namespace RoutingSample
{
    public class HttpContextRouteEndpoint : IRouter
    {
        private readonly RequestDelegate _appFunc;

        public HttpContextRouteEndpoint(RequestDelegate appFunc)
        {
            _appFunc = appFunc;
        }

        public async Task RouteAsync(RouteContext context)
        {
            await _appFunc(context.HttpContext);
            context.IsHandled = true;
        }

        public string BindPath(BindPathContext context)
        {
            // We don't really care what the values look like.
            context.IsBound = true;
            return null;
        }
    }
}
