// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// IApplicationBuilder extension methods for the ErrorPageMiddleware.
    /// </summary>
    public static class DeveloperExceptionPageExtensions
    {
        /// <summary>
        /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
        /// Full error details are only displayed by default if 'host.AppMode' is set to 'development' in the IApplicationBuilder.Properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseDeveloperExceptionPage(new ErrorPageOptions());
        }

        /// <summary>
        /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
        /// Full error details are only displayed by default if 'host.AppMode' is set to 'development' in the IApplicationBuilder.Properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder builder, ErrorPageOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<DeveloperExceptionPageMiddleware>(options);
        }
    }
}
