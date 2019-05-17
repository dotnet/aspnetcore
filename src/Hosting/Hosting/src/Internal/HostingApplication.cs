// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    internal class HostingApplication : IHttpApplication<HostingApplication.Context>
    {
        private readonly RequestDelegate _application;
        private readonly IHttpContextFactory _httpContextFactory;
        private HostingApplicationDiagnostics _diagnostics;

        public HostingApplication(
            RequestDelegate application,
            ILogger logger,
            DiagnosticListener diagnosticSource,
            IHttpContextFactory httpContextFactory)
        {
            _application = application;
            _diagnostics = new HostingApplicationDiagnostics(logger, diagnosticSource);
            _httpContextFactory = httpContextFactory;
        }

        // Set up the request
        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            var httpContext = _httpContextFactory.Create(contextFeatures);

            Context hostContext;
            if (contextFeatures is IHostContextContainer<Context> container)
            {
                // Initalize the wrapper struct in-place; so its object reference gets set
                container.HostContext.Initialize();
                // Now we can copy it
                hostContext = container.HostContext;
            }
            else
            {
                hostContext = Context.Create();
            }

            hostContext.HttpContext = httpContext;
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
            _httpContextFactory.Dispose(httpContext);
            _diagnostics.ContextDisposed(context);

            context.Reset();
        }

        // Struct to turn {TContext} uses into faster concrete generic implementations rather than shared generics.
        internal struct Context
        {
            // Single field wrapper over an object to keep pass-by-value sematics in a single pointer register
            // rather than involving stack copies.
            private InnerContext _context;

            internal static Context Create()
            {
                return new Context() { _context = new InnerContext() };
            }

            internal Context Initialize()
            {
                _context ??= new InnerContext();
                return this;
            }

            internal void Reset() => _context.Reset();

            public HttpContext HttpContext
            {
                get => _context.HttpContext;
                set => _context.HttpContext = value;
            }

            public IDisposable Scope
            {
                get => _context.Scope;
                set => _context.Scope = value;
            }

            public long StartTimestamp
            {
                get => _context.StartTimestamp;
                set => _context.StartTimestamp = value;
            }

            public bool EventLogEnabled
            {
                get => _context.EventLogEnabled;
                set => _context.EventLogEnabled = value;
            }

            public Activity Activity
            {
                get => _context.Activity;
                set => _context.Activity = value;
            }

            internal bool HasDiagnosticListener
            {
                get => _context.HasDiagnosticListener;
                set => _context.HasDiagnosticListener = value;
            }

            private class InnerContext
            {
                public HttpContext HttpContext { get; set; }
                public IDisposable Scope { get; set; }
                public Activity Activity { get; set; }

                public long StartTimestamp { get; set; }
                internal bool HasDiagnosticListener { get; set; }
                public bool EventLogEnabled { get; set; }

                public void Reset()
                {
                    HttpContext = null;
                    Scope = null;
                    Activity = null;

                    StartTimestamp = 0;
                    HasDiagnosticListener = false;
                    EventLogEnabled = false;
                }
            }
        }
    }
}
