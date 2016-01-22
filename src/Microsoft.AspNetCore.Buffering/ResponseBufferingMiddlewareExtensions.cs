// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Buffering;

namespace Microsoft.AspNetCore.Builder
{
    public static class ResponseBufferingMiddlewareExtensions
    {
        /// <summary>
        /// Enables full buffering of response bodies. This can be disabled on a per request basis using IHttpBufferingFeature.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseResponseBuffering(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseBufferingMiddleware>();
        }
    }
}
