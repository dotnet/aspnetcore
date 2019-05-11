// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using HostContext = Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingApplication : IHttpApplication<HostContext>
    {
        private readonly RequestDelegate _application;
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly HostingApplicationDiagnostics _diagnostics;

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
        public HostContext CreateContext(IFeatureCollection contextFeatures)
        {
            var httpContext = _httpContextFactory.Create(contextFeatures);

            if (contextFeatures is IContextContainer<HostContext> container &&
                container.TryGetContext(out var hostContext))
            {
                hostContext.Initialize();
            }
            else
            {
                hostContext = HostContext.Create();
            }

            hostContext.HttpContext = httpContext;
            _diagnostics.BeginRequest(httpContext, hostContext);

            return hostContext;
        }

        // Execute the request
        public Task ProcessRequestAsync(HostContext context)
        {
            return _application(context.HttpContext);
        }

        // Clean up the request
        public void DisposeContext(HostContext context, Exception exception)
        {
            var httpContext = context.HttpContext;
            var container = httpContext.Features as IContextContainer<HostContext>;

            _diagnostics.RequestEnd(httpContext, exception, context);
            _httpContextFactory.Dispose(httpContext);
            _diagnostics.ContextDisposed(context);

            if (container is object)
            {
                context.Reset();
                container.ReleaseContext(context);
            }
        }

        // Struct to turn {TContext} uses into faster concrete generic implementations rather than shared generics.
        public struct Context
        {
            // Single field wrapper over an object to keep pass-by-value sematics in a single pointer register
            // rather than involving stack copies.
            private InnerContext _context;

            public static HostContext Create()
            {
                return new HostContext() { _context = new InnerContext() };
            }

            internal HostContext Initialize()
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

            internal HostingRequestStartingLog StartLog
            {
                get => _context.StartLog;
                set => _context.StartLog = value;
            }

            private class InnerContext
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
                    HttpContext = null;
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
}
