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
            var startTick = Environment.TickCount;

            var scope = _logger.RequestScope(httpContext);
            _logger.RequestStarting(httpContext);
            if (_diagnosticSource.IsEnabled("Microsoft.AspNet.Hosting.BeginRequest"))
            {
                _diagnosticSource.Write("Microsoft.AspNet.Hosting.BeginRequest", new { httpContext = httpContext, tickCount = startTick });
            }

            return new Context
            {
                HttpContext = httpContext,
                Scope = scope,
                StartTick = startTick,
            };
        }

        public void DisposeContext(Context context, Exception exception)
        {
            var httpContext = context.HttpContext;
            var currentTick = Environment.TickCount;
            _logger.RequestFinished(httpContext, context.StartTick, currentTick);

            if (exception == null)
            {
                if (_diagnosticSource.IsEnabled("Microsoft.AspNet.Hosting.EndRequest"))
                {
                    _diagnosticSource.Write("Microsoft.AspNet.Hosting.EndRequest", new { httpContext = httpContext, tickCount = currentTick });
                }
            }
            else
            {
                if (_diagnosticSource.IsEnabled("Microsoft.AspNet.Hosting.UnhandledException"))
                {
                    _diagnosticSource.Write("Microsoft.AspNet.Hosting.UnhandledException", new { httpContext = httpContext, tickCount = currentTick, exception = exception });
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
            public int StartTick { get; set; }
        }
    }
}
