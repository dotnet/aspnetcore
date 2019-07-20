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
    internal class HostingApplication : IHttpApplication<HostingApplication.ContextWrapper>
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
        public ContextWrapper CreateContext(IFeatureCollection contextFeatures)
        {
            Context hostContext;
            if (contextFeatures is IHostContextContainer<ContextWrapper> container)
            {
                hostContext = container.HostContext.Context;
                if (hostContext is null)
                {
                    hostContext = new Context();
                    // Initalize the wrapper struct in-place; so its wrapped object reference gets set
                    container.HostContext = new ContextWrapper(hostContext);
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
            return new ContextWrapper(hostContext);
        }

        // Execute the request
        public Task ProcessRequestAsync(ContextWrapper contextWrapper)
        {
            return _application(contextWrapper.Context.HttpContext);
        }

        // Clean up the request
        public void DisposeContext(ContextWrapper contextWrapper, Exception exception)
        {
            var context = contextWrapper.Context;
            var httpContext = context.HttpContext;
            _diagnostics.RequestEnd(httpContext, exception, context);

            if (_defaultHttpContextFactory != null)
            {
                _defaultHttpContextFactory.Dispose((DefaultHttpContext)httpContext);
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

            public long StartTimestamp { get; set; }
            internal bool HasDiagnosticListener { get; set; }
            public bool EventLogEnabled { get; set; }

            public void Reset()
            {
                Scope = null;
                Activity = null;

                StartTimestamp = 0;
                HasDiagnosticListener = false;
                EventLogEnabled = false;
            }
        }

        /// <summary>
        /// Struct wrapper to devirtualize <see cref="IHttpApplication{TContext}"/> into faster concrete generic implementations rather than shared generics.
        /// </summary>
        internal readonly struct ContextWrapper
        {
            public ContextWrapper(Context context)
            {
                Context = context;
            }

            public Context Context { get; }
        }
    }
}
