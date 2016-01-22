// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.MiddlewareAnalysis
{
    public class AnalysisMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DiagnosticSource _diagnostics;
        private readonly string _middlewareName;

        public AnalysisMiddleware(RequestDelegate next, DiagnosticSource diagnosticSource, string middlewareName)
        {
            _next = next;
            _diagnostics = diagnosticSource;
            if (string.IsNullOrEmpty(middlewareName))
            {
                middlewareName = next.Target.GetType().FullName;
            }
            _middlewareName = middlewareName;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (_diagnostics.IsEnabled("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareStarting"))
            {
                _diagnostics.Write("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareStarting", new { name = _middlewareName, httpContext = httpContext });
            }

            try
            {
                await _next(httpContext);

                if (_diagnostics.IsEnabled("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareFinished"))
                {
                    _diagnostics.Write("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareFinished", new { name = _middlewareName, httpContext = httpContext });
                }
            }
            catch (Exception ex)
            {
                if (_diagnostics.IsEnabled("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareException"))
                {
                    _diagnostics.Write("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareException", new { name = _middlewareName, httpContext = httpContext, exception = ex });
                }
                throw;
            }
        }
    }
}
