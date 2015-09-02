// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods provided by the claims transformation authentication middleware
    /// </summary>
    public static class ClaimsTransformationAppBuilderExtensions
    {
        /// <summary>
        /// Adds a claims transformation middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app)
        {
            return app.UseClaimsTransformation(configureOptions: o => { });
        }

        /// <summary>
        /// Adds a claims transformation middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <param name="configureOptions">Used to configure the options for the middleware</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseClaimsTransformation(this IApplicationBuilder app, [NotNull] Action<ClaimsTransformationOptions> configureOptions)
        {
            return app.UseMiddleware<ClaimsTransformationMiddleware>(
                new ConfigureOptions<ClaimsTransformationOptions>(configureOptions));
        }
    }
}