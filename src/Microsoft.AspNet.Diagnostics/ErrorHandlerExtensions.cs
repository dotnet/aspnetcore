// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    public static class ErrorHandlerExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, reset the request path, and re-execute the request.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="errorHandlingPath"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app, string errorHandlingPath)
        {
            var options = new ErrorHandlerOptions()
            {
                ErrorHandlingPath = new PathString(errorHandlingPath)
            };
            return app.UseMiddleware<ErrorHandlerMiddleware>(options);
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app, Action<IApplicationBuilder> configure)
        {
            var subAppBuilder = app.New();
            configure(subAppBuilder);
            var errorPipeline = subAppBuilder.Build();
            var options = new ErrorHandlerOptions()
            {
                ErrorHandler = errorPipeline
            };
            return app.UseMiddleware<ErrorHandlerMiddleware>(options);
        }
    }
}