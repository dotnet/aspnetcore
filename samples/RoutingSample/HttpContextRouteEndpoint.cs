﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;

namespace RoutingSample
{
    public class HttpContextRouteEndpoint : IRouteEndpoint
    {
        private readonly Func<HttpContext, Task> _appFunc;

        public HttpContextRouteEndpoint(Func<HttpContext, Task> appFunc)
        {
            _appFunc = appFunc;
        }

        public async Task<bool> Send(HttpContext context)
        {
            await _appFunc(context);
            return true;
        }
    }
}
