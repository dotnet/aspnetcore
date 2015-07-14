// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// IApplicationBuilder extension methods for the ErrorPageMiddleware.
    /// </summary>
    public static class ErrorPageExtensions
    {
        /// <summary>
        /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
        /// Full error details are only displayed by default if 'host.AppMode' is set to 'development' in the IApplicationBuilder.Properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseErrorPage([NotNull] this IApplicationBuilder builder)
        {
            return builder.UseErrorPage(new ErrorPageOptions());
        }

        /// <summary>
        /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
        /// Full error details are only displayed by default if 'host.AppMode' is set to 'development' in the IApplicationBuilder.Properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseErrorPage([NotNull] this IApplicationBuilder builder, ErrorPageOptions options)
        {
            return builder.UseMiddleware<ErrorPageMiddleware>(options);
        }
    }
}
