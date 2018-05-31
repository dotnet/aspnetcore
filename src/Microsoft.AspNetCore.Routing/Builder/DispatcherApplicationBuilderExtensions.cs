// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class DispatcherApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDispatcher(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DispatcherMiddleware>();
        }

        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EndpointMiddleware>();
        }
    }
}
