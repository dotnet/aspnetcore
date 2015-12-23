// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    public static class ExceptionHandlerExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, reset the request path, and re-execute the request.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="errorHandlingPath"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, string errorHandlingPath)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseExceptionHandler(options => { options.ExceptionHandlingPath = new PathString(errorHandlingPath); });
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, Action<IApplicationBuilder> configure)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var subAppBuilder = app.New();
            configure(subAppBuilder);
            var exceptionHandlerPipeline = subAppBuilder.Build();

            return app.UseExceptionHandler(options => { options.ExceptionHandler = exceptionHandlerPipeline; });
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, Action<ExceptionHandlerOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new ExceptionHandlerOptions();
            configureOptions(options);

            return app.UseMiddleware<ExceptionHandlerMiddleware>(options);
        }
    }
}