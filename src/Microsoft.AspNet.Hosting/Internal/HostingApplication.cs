// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting.Internal
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
            var diagnoticsEnabled = _diagnosticSource.IsEnabled("Microsoft.AspNet.Hosting.BeginRequest");
            var startTimestamp = (diagnoticsEnabled || _logger.IsEnabled(LogLevel.Information)) ? Stopwatch.GetTimestamp() : 0;

            var scope = _logger.RequestScope(httpContext);
            _logger.RequestStarting(httpContext);
            if (diagnoticsEnabled)
            {
                _diagnosticSource.Write("Microsoft.AspNet.Hosting.BeginRequest", new { httpContext = httpContext, timestamp = startTimestamp });
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

            if (exception == null)
            {
                var diagnoticsEnabled = _diagnosticSource.IsEnabled("Microsoft.AspNet.Hosting.EndRequest");
                var currentTimestamp = (diagnoticsEnabled || context.StartTimestamp != 0) ? Stopwatch.GetTimestamp() : 0;

                _logger.RequestFinished(httpContext, context.StartTimestamp, currentTimestamp);

                if (diagnoticsEnabled)
                {
                    _diagnosticSource.Write("Microsoft.AspNet.Hosting.EndRequest", new { httpContext = httpContext, timestamp = currentTimestamp });
                }
            }
            else
            {
                var diagnoticsEnabled = _diagnosticSource.IsEnabled("Microsoft.AspNet.Hosting.UnhandledException");
                var currentTimestamp = (diagnoticsEnabled || context.StartTimestamp != 0) ? Stopwatch.GetTimestamp() : 0;

                _logger.RequestFinished(httpContext, context.StartTimestamp, currentTimestamp);

                if (diagnoticsEnabled)
                {
                    _diagnosticSource.Write("Microsoft.AspNet.Hosting.UnhandledException", new { httpContext = httpContext, timestamp = currentTimestamp, exception = exception });
                }
            }

            context.Scope.Dispose();

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
