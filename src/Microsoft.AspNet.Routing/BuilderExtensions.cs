// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Builder
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseRouter(this IApplicationBuilder builder, IRouter router)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            return builder.UseMiddleware<RouterMiddleware>(router);
        }
    }
}