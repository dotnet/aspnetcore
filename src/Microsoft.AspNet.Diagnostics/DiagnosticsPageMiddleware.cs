// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


#if DEBUG
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// A human readable page with basic debugging actions.
    /// </summary>
    public class DiagnosticsPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DiagnosticsPageOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsPageMiddleware"/> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public DiagnosticsPageMiddleware(RequestDelegate next, DiagnosticsPageOptions options)
        {
            _next = next;
            _options = options;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            if (!_options.Path.HasValue || _options.Path == context.Request.Path)
            {
                var page = new DiagnosticsPage();
                page.Execute(context);
                return Task.FromResult(0);
            }
            return _next(context);
        }
    }
}
#endif