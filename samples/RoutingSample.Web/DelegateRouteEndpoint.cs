// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        public string GetVirtualPath(VirtualPathContext context)
        {
            // We don't really care what the values look like.
            context.IsBound = true;
            return null;
        }
    }
}
