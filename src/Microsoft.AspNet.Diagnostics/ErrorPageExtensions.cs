// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// IBuilder extension methods for the ErrorPageMiddleware.
    /// </summary>
    public static class ErrorPageExtensions
    {
        /// <summary>
        /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
        /// Full error details are only displayed by default if 'host.AppMode' is set to 'development' in the IBuilder.Properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseErrorPage(this IBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.UseErrorPage(new ErrorPageOptions());
        }

        /// <summary>
        /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
        /// Full error details are only displayed by default if 'host.AppMode' is set to 'development' in the IBuilder.Properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseErrorPage(this IBuilder builder, ErrorPageOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            /* TODO: Development, Staging, or Production
            string appMode = new AppProperties(builder.Properties).Get<string>(Constants.HostAppMode);
            bool isDevMode = string.Equals(Constants.DevMode, appMode, StringComparison.Ordinal);*/
            bool isDevMode = true;
            return builder.Use(next => new ErrorPageMiddleware(next, options, isDevMode).Invoke);
        }
    }
}