// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CorsMiddlewareWebSite
{
    public class EchoMiddleware
    {
        /// <summary>
        /// Instantiates a new <see cref="EchoMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public EchoMiddleware(RequestDelegate next)
        {
        }

        /// <summary>
        /// Echo the request's path in the response. Does not invoke later middleware in the pipeline.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the current request.</param>
        /// <returns>A <see cref="Task"/> that completes when writing to the response is done.</returns>
        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Response.ContentType = "text/plain; charset=utf-8";
            var path = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            return context.Response.WriteAsync(path, Encoding.UTF8);
        }
    }
}