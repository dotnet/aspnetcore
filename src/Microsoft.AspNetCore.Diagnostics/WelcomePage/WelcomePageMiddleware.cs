// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// This middleware provides a default web page for new applications.
    /// </summary>
    public class WelcomePageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WelcomePageOptions _options;

        /// <summary>
        /// Creates a default web page for new applications.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public WelcomePageMiddleware(RequestDelegate next, IOptions<WelcomePageOptions> options)
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
        /// Process an individual request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            HttpRequest request = context.Request;
            if (!_options.Path.HasValue || _options.Path == request.Path)
            {
                // Dynamically generated for LOC.
                var welcomePage = new WelcomePage();
                return welcomePage.ExecuteAsync(context);
            }

            return _next(context);
        }
    }
}
