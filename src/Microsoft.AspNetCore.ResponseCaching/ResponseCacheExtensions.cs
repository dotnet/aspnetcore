// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class ResponseCacheExtensions
    {
        public static IApplicationBuilder UseResponseCache(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ResponseCacheMiddleware>();
        }

        public static IApplicationBuilder UseResponseCache(this IApplicationBuilder app, ResponseCacheOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<ResponseCacheMiddleware>(Options.Create(options));
        }
    }
}
