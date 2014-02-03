﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.PipelineCore.Owin;
using Microsoft.AspNet.Routing;

namespace RoutingSample
{
    internal class OwinRouteEndpoint : IRouteEndpoint
    {
        private readonly Func<IDictionary<string, object>, Task> _appFunc;

        public OwinRouteEndpoint(Func<IDictionary<string, object>, Task> appFunc)
        {
            _appFunc = appFunc;
        }

        public async Task<bool> Send(HttpContext context)
        {
            var owinContext = context.GetFeature<ICanHasOwinEnvironment>().Environment;
            return _appFunc(owinContext);
            await _appFunc(owinContext);
            return true;
        }
    }
}

#endif
