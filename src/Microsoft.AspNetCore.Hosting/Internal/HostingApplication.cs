// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingApplication : IHttpApplication<HostingApplication.Context>
    {
        private readonly RequestDelegate _application;
        private readonly ILogger _logger;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly IHttpContextFactory _httpContextFactory;

        public HostingApplication(
            RequestDelegate application,
            ILogger logger,
            DiagnosticSource diagnosticSource,
            IHttpContextFactory httpContextFactory)
        {
            _application = application;
            _logger = logger;
            _diagnosticSource = diagnosticSource;
            _httpContextFactory = httpContextFactory;
        }

        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            var httpContext = _httpContextFactory.Create(contextFeatures);
            var diagnoticsEnabled = _diagnosticSource.IsEnabled("Microsoft.AspNetCore.Hosting.BeginRequest");
            var startTimestamp = (diagnoticsEnabled || _logger.IsEnabled(LogLevel.Information)) ? Stopwatch.GetTimestamp() : 0;

            var scope = _logger.RequestScope(httpContext);
            _logger.RequestStarting(httpContext);
            if (diagnoticsEnabled)
            {
                _diagnosticSource.Write("Microsoft.AspNetCore.Hosting.BeginRequest", new { httpContext = httpContext, timestamp = startTimestamp });
            }

            var hostingLog = HostingEventSource.Log;
            if (hostingLog.IsEnabled())
            {
                hostingLog.RequestStart(httpContext.Request.Method, httpContext.Request.Path);
            }

            return new Context
            {
                HttpContext = httpContext,
                Scope = scope,
                StartTimestamp = startTimestamp,
            };
        }

        public void DisposeContext(Context context, Exception exception)
        {
            var httpContext = context.HttpContext;
            var hostingLog = HostingEventSource.Log;
            if (exception == null)
            {
                var diagnoticsEnabled = _diagnosticSource.IsEnabled("Microsoft.AspNetCore.Hosting.EndRequest");
                var currentTimestamp = (diagnoticsEnabled || context.StartTimestamp != 0) ? Stopwatch.GetTimestamp() : 0;

                _logger.RequestFinished(httpContext, context.StartTimestamp, currentTimestamp);

                if (diagnoticsEnabled)
                {
                    _diagnosticSource.Write("Microsoft.AspNetCore.Hosting.EndRequest", new { httpContext = httpContext, timestamp = currentTimestamp });
                }
            }
            else
            {
                var diagnoticsEnabled = _diagnosticSource.IsEnabled("Microsoft.AspNetCore.Hosting.UnhandledException");
                var currentTimestamp = (diagnoticsEnabled || context.StartTimestamp != 0) ? Stopwatch.GetTimestamp() : 0;

                _logger.RequestFinished(httpContext, context.StartTimestamp, currentTimestamp);

                if (diagnoticsEnabled)
                {
                    _diagnosticSource.Write("Microsoft.AspNetCore.Hosting.UnhandledException", new { httpContext = httpContext, timestamp = currentTimestamp, exception = exception });
                }

                if (hostingLog.IsEnabled())
                {
                    hostingLog.UnhandledException();
                }
            }

            if (hostingLog.IsEnabled())
            {
                hostingLog.RequestStop();
            }

            context.Scope?.Dispose();

            _httpContextFactory.Dispose(httpContext);
        }

        public Task ProcessRequestAsync(Context context)
        {
            return _application(context.HttpContext);
        }

        public struct Context
        {
            public HttpContext HttpContext { get; set; }
            public IDisposable Scope { get; set; }
            public long StartTimestamp { get; set; }
        }
    }
}
