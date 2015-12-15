// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="DeveloperExceptionPageMiddleware"/>.
    /// </summary>
    public static class DeveloperExceptionPageExtensions
    {
        /// <summary>
        /// Captures synchronous and asynchronous <see cref="Exception"/> instances from the pipeline and generates HTML error responses.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A reference to the <paramref name="builder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseDeveloperExceptionPage(options => { });
        }

        /// <summary>
        /// Captures synchronous and asynchronous <see cref="Exception"/> instances from the pipeline and generates HTML error responses.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A callback to configure <see cref="ErrorPageOptions"/>.</param>
        /// <returns>A reference to the <paramref name="builder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseDeveloperExceptionPage(
            this IApplicationBuilder builder,
            Action<ErrorPageOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new ErrorPageOptions();
            configureOptions(options);
            return builder.UseMiddleware<DeveloperExceptionPageMiddleware>(options);
        }
    }
}
