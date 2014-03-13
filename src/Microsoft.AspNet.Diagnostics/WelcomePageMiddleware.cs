// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.Abstractions;

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
        public WelcomePageMiddleware(RequestDelegate next, WelcomePageOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            _next = next;
            _options = options;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            HttpRequest request = context.Request;
            if (!_options.Path.HasValue || _options.Path == request.Path)
            {
                // Dynamically generated for LOC.
                var welcomePage = new WelcomePage();
                welcomePage.Execute(context);
                return Task.FromResult(0);
            }

            return _next(context);
        }
    }
}
