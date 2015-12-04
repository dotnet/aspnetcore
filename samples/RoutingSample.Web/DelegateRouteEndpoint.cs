// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    public class DelegateRouteEndpoint : IRouter
    {
        public delegate Task RoutedDelegate(RouteContext context);

        private readonly RoutedDelegate _appFunc;

        public DelegateRouteEndpoint(RoutedDelegate appFunc)
        {
            _appFunc = appFunc;
        }

        public async Task RouteAsync(RouteContext context)
        {
            await _appFunc(context);
            context.IsHandled = true;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }
    }
}
