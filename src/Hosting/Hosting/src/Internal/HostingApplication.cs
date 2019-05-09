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
        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            var httpContext = _httpContextFactory.Create(contextFeatures);

            var context = new Context() { HttpContext = httpContext };
            _diagnostics.BeginRequest(httpContext, ref context);

            return context;
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
        }

        public struct Context
        {
            private LogContext _loggingContext;

            public HttpContext HttpContext { get; set; }
            public IDisposable Scope
            {
                // Get set value or return null
                get => _loggingContext?.Scope;
                set
                {
                    if (value == null)
                    {
                        // If null, only set to null if the _loggingContext exists as the get will default to null.
                        if (_loggingContext != null)
                        {
                            _loggingContext.Scope = null;
                        }
                    }
                    else
                    {
                        // Not null, so create the LoggingContext if it doesn't exist and set the value.
                        LoggingContext.Scope = value;
                    }
                }
            }

            public long StartTimestamp
            {
                // Get set value or return zero
                get => _loggingContext?.StartTimestamp ?? 0;
                set
                {
                    if (value == 0)
                    {
                        // If zero, only set to zero if the _loggingContext exists as the get will default to zero.
                        if (_loggingContext != null)
                        {
                            _loggingContext.StartTimestamp = 0;
                        }
                    }
                    else
                    {
                        // Not zero, so create the LoggingContext if it doesn't exist and set the value.
                        LoggingContext.StartTimestamp = value;
                    }
                }
            }

            public bool EventLogEnabled
            {
                // Get set value or return false
                get => _loggingContext?.EventLogEnabled ?? false;
                set
                {
                    if (!value)
                    {
                        // If false, only set to false if the _loggingContext exists as the get will default to false.
                        if (_loggingContext != null)
                        {
                            _loggingContext.EventLogEnabled = false;
                        }
                    }
                    else
                    {
                        // Not false, so create the LoggingContext if it doesn't exist and set to true.
                        LoggingContext.EventLogEnabled = true;
                    }
                }
            }

            public Activity Activity
            {
                // Get set value or return null
                get => _loggingContext?.Activity;
                set
                {
                    if (value == null)
                    {
                        // If null, only set to null if the _loggingContext exists as the get will default to null.
                        if (_loggingContext != null)
                        {
                            _loggingContext.Activity = null;
                        }
                    }
                    else
                    {
                        // Not null, so create the LoggingContext if it doesn't exist and set the value.
                        LoggingContext.Activity = value;
                    }
                }
            }

            internal bool HasDiagnosticListener
            {
                // Get set value or return false
                get => _loggingContext?.HasDiagnosticListener ?? false;
                set
                {
                    if (!value)
                    {
                        // If false, only set to false if the _loggingContext exists as the get will default to false.
                        if (_loggingContext != null)
                        {
                            _loggingContext.HasDiagnosticListener = false;
                        }
                    }
                    else
                    {
                        // Not false, so create the LoggingContext if it doesn't exist and set to true.
                        LoggingContext.HasDiagnosticListener = true;
                    }
                }
            }

            internal HostingRequestStartingLog StartLog
            {
                // Get set value or return null
                get => _loggingContext?.StartLog;
                set
                {
                    if (value == null)
                    {
                        // If null, only set to null if the _loggingContext exists as the get will default to null.
                        if (_loggingContext != null)
                        {
                            _loggingContext.StartLog = null;
                        }
                    }
                    else
                    {
                        // Not null, so create the LoggingContext if it doesn't exist and set the value.
                        LoggingContext.StartLog = value;
                    }
                }
            }

            // Get or create the _loggingContext
            private LogContext LoggingContext => _loggingContext ??= new LogContext();

            private class LogContext
            {
                public IDisposable Scope { get; set; }
                public long StartTimestamp { get; set; }
                public bool EventLogEnabled { get; set; }
                public Activity Activity { get; set; }
                internal bool HasDiagnosticListener { get; set; }
                internal HostingRequestStartingLog StartLog { get; set; }
            }
        }
    }
}
