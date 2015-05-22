// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.ResponseCaching;

namespace Microsoft.AspNet.Builder
{
    public static class ResponseCachingExtensions
    {
        public static IApplicationBuilder UseResponseCaching(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ResponseCachingMiddleware>();
        }
    }
}
