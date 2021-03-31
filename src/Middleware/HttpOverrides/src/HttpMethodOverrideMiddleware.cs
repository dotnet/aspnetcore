// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpOverrides
{
    /// <summary>
    /// A middleware for overriding the HTTP method of an incoming POST request.
    /// </summary>
    public class HttpMethodOverrideMiddleware
    {
        private const string xHttpMethodOverride = "X-Http-Method-Override";
        private readonly RequestDelegate _next;
        private readonly HttpMethodOverrideOptions _options;

        /// <summary>
        /// Create a new <see cref="HttpMethodOverrideMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The <see cref="HttpMethodOverrideOptions"/> for configuring the middleware.</param>
        public HttpMethodOverrideMiddleware(RequestDelegate next, IOptions<HttpMethodOverrideOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _next = next;
            _options = options.Value;
        }

        /// <summary>
        /// Executes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        public async Task Invoke(HttpContext context)
        {
            if (HttpMethods.IsPost(context.Request.Method))
            {
                if (_options.FormFieldName != null)
                {
                    if (context.Request.HasFormContentType)
                    {
                        var form = await context.Request.ReadFormAsync();
                        var methodType = form[_options.FormFieldName];
                        if (!string.IsNullOrEmpty(methodType))
                        {
                            context.Request.Method = methodType;
                        }
                    }
                }
                else
                {
                    var xHttpMethodOverrideValue = context.Request.Headers[xHttpMethodOverride];
                    if (!string.IsNullOrEmpty(xHttpMethodOverrideValue))
                    {
                        context.Request.Method = xHttpMethodOverrideValue;
                    }
                }
            }
            await _next(context);
        }
    }
}
