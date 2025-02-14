// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HostingApplication : IHttpApplication<HostingApplication.Context>
{
    private readonly RequestDelegate _application;
    private readonly IHttpContextFactory? _httpContextFactory;
    private readonly DefaultHttpContextFactory? _defaultHttpContextFactory;
    private readonly HostingApplicationDiagnostics _diagnostics;

    public HostingApplication(
        RequestDelegate application,
        ILogger logger,
        DiagnosticListener diagnosticSource,
        ActivitySource activitySource,
        DistributedContextPropagator propagator,
        IHttpContextFactory httpContextFactory,
        HostingEventSource eventSource,
        HostingMetrics metrics)
    {
        _application = application;
        _diagnostics = new HostingApplicationDiagnostics(logger, diagnosticSource, activitySource, propagator, eventSource, metrics);
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
        Context? hostContext;
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
            var defaultHttpContext = (DefaultHttpContext?)hostContext.HttpContext;
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
            httpContext = _httpContextFactory!.Create(contextFeatures);
            hostContext.HttpContext = httpContext;
        }

        _diagnostics.BeginRequest(httpContext, hostContext);
        return hostContext;
    }

    // Execute the request
    public Task ProcessRequestAsync(Context context)
    {
        return _application(context.HttpContext!);
    }

    // Clean up the request
    public void DisposeContext(Context context, Exception? exception)
    {
        var httpContext = context.HttpContext!;
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
            _httpContextFactory!.Dispose(httpContext);
        }

        _diagnostics.ContextDisposed(context);

        // Reset the context as it may be pooled
        context.Reset();
    }

    internal sealed class Context
    {
        public HttpContext? HttpContext { get; set; }
        public IDisposable? Scope { get; set; }
        public Activity? Activity
        {
            get => HttpActivityFeature?.Activity;
            set
            {
                if (HttpActivityFeature is null)
                {
                    if (value != null)
                    {
                        HttpActivityFeature = new HttpActivityFeature(value);
                    }
                }
                else
                {
                    HttpActivityFeature.Activity = value!;
                }
            }
        }
        internal HostingRequestStartingLog? StartLog { get; set; }

        public long StartTimestamp { get; set; }
        internal bool HasDiagnosticListener { get; set; }
        public bool MetricsEnabled { get; set; }
        public bool EventLogEnabled { get; set; }

        internal HttpActivityFeature? HttpActivityFeature;
        internal HttpMetricsTagsFeature? MetricsTagsFeature;

        public void Reset()
        {
            // Not resetting HttpContext here as we pool it on the Context

            Scope = null;
            Activity = null;
            StartLog = null;

            StartTimestamp = 0;
            HasDiagnosticListener = false;
            MetricsEnabled = false;
            EventLogEnabled = false;
            MetricsTagsFeature?.Reset();
        }
    }
}
