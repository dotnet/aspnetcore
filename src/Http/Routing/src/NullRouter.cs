// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Routing
{
    internal class NullRouter : IRouter
    {
        public static readonly NullRouter Instance = new NullRouter();

        private NullRouter()
        {
        }

        public VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            return Task.CompletedTask;
        }
    }
}
