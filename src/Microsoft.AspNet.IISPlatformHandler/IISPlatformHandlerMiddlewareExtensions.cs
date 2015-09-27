// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.IISPlatformHandler;

namespace Microsoft.AspNet.Builder
{
    public static class IISPlatformHandlerMiddlewareExtensions
    {
        /// <summary>
        /// Adds middleware for interacting with the IIS HttpPlatformHandler reverse proxy module.
        /// This will handle forwarded Windows Authentication, request scheme, remote IPs, etc..
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIISPlatformHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IISPlatformHandlerMiddleware>();
        }
    }
}
