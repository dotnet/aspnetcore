// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    internal class HostingApplication : IHttpApplication<HostingApplication.Context>
    {
        private readonly RequestDelegate _application;
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly DefaultHttpContextFactory _defaultHttpContextFactory;
        private HostingApplicationDiagnostics _diagnostics;

        public HostingApplication(
            RequestDelegate application,
            ILogger logger,
            DiagnosticListener diagnosticSource,
            IHttpContextFactory httpContextFactory)
        {
            _application = application;
            _diagnostics = new HostingApplicationDiagnostics(logger, diagnosticSource);
            if (httpContextFactory is DefaultHttpContextFactory factory)
            {
                _defaultHttpContextFactory = factory;
            }
            else
            {
                _httpContextFactory = httpContextFactory;
            }
        }

        // Set up the request
        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            Context hostContext;
            if (contextFeatures is IHostContextContainer<Context> container)
            {
                hostContext = container.HostContext;
                if (hostContext is null)
                {
                    hostContext = new Context();
                    container.HostContext = hostContext;
                }
            }
            else
            {
                // Server doesn't support pooling, so create a new Context
                hostContext = new Context();
            }

            HttpContext httpContext;
            if (_defaultHttpContextFactory != null)
            {
                var defaultHttpContext = (DefaultHttpContext)hostContext.HttpContext;
                if (defaultHttpContext is null)
                {
                    httpContext = _defaultHttpContextFactory.Create(contextFeatures);
                    hostContext.HttpContext = httpContext;
                }
                else
                {
                    _defaultHttpContextFactory.Initialize(defaultHttpContext, contextFeatures);
                    httpContext = defaultHttpContext;
                }
            }
            else
            {
                httpContext = _httpContextFactory.Create(contextFeatures);
                hostContext.HttpContext = httpContext;
            }

            _diagnostics.BeginRequest(httpContext, hostContext);
            return hostContext;
        }

        // Execute the request
        public Task ProcessRequestAsync(Context context)
        {
            return _application(context.HttpContext);
        }

        // Clean up the request
        public void DisposeContext(Context context, Exception exception)
        {
            var httpContext = context.HttpContext;
            _diagnostics.RequestEnd(httpContext, exception, context);

            if (_defaultHttpContextFactory != null)
            {
                _defaultHttpContextFactory.Dispose((DefaultHttpContext)httpContext);

                if (_defaultHttpContextFactory.HttpContextAccessor != null)
                {
                    // Clear the HttpContext if the accessor was used. It's likely that the lifetime extends
                    // past the end of the http request and we want to avoid changing the reference from under
                    // consumers.
                    context.HttpContext = null;
                }
            }
            else
            {
                _httpContextFactory.Dispose(httpContext);
            }

            _diagnostics.ContextDisposed(context);

            // Reset the context as it may be pooled
            context.Reset();
        }


        internal class Context
        {
            public HttpContext HttpContext { get; set; }
            public IDisposable Scope { get; set; }
            public Activity Activity { get; set; }
            internal HostingRequestStartingLog StartLog { get; set; }

            public long StartTimestamp { get; set; }
            internal bool HasDiagnosticListener { get; set; }
            public bool EventLogEnabled { get; set; }

            public void Reset()
            {
                // Not resetting HttpContext here as we pool it on the Context

                Scope = null;
                Activity = null;
                StartLog = null;

                StartTimestamp = 0;
                HasDiagnosticListener = false;
                EventLogEnabled = false;
            }
        }
    }
}
