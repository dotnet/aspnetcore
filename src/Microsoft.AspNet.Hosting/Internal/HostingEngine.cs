// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class HostingEngine : IHostingEngine
    {
        // This is defined by IIS's HttpPlatformHandler.
        private static readonly string ServerPort = "HTTP_PLATFORM_PORT";
        private static readonly string DetailedErrors = "Hosting:DetailedErrors";

        private readonly IServiceCollection _applicationServiceCollection;
        private readonly IStartupLoader _startupLoader;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly IConfiguration _config;
        private readonly bool _captureStartupErrors;

        private IServiceProvider _applicationServices;

        // Only one of these should be set
        internal string StartupAssemblyName { get; set; }
        internal StartupMethods Startup { get; set; }
        internal Type StartupType { get; set; }

        // Only one of these should be set
        internal IServerFactory ServerFactory { get; set; }
        internal string ServerFactoryLocation { get; set; }
        private IFeatureCollection _serverInstance;

        public HostingEngine(
            IServiceCollection appServices,
            IStartupLoader startupLoader,
            IConfiguration config,
            bool captureStartupErrors)
        {
            if (appServices == null)
            {
                throw new ArgumentNullException(nameof(appServices));
            }

            if (startupLoader == null)
            {
                throw new ArgumentNullException(nameof(startupLoader));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _applicationServiceCollection = appServices;
            _startupLoader = startupLoader;
            _captureStartupErrors = captureStartupErrors;
            _applicationLifetime = new ApplicationLifetime();
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                EnsureApplicationServices();
                return _applicationServices;
            }
        }

        public virtual IApplication Start()
        {
            var application = BuildApplication();

            var logger = _applicationServices.GetRequiredService<ILogger<HostingEngine>>();
            var contextFactory = _applicationServices.GetRequiredService<IHttpContextFactory>();
            var contextAccessor = _applicationServices.GetRequiredService<IHttpContextAccessor>();
#pragma warning disable 0618
            var telemetrySource = _applicationServices.GetRequiredService<TelemetrySource>();
#pragma warning restore 0618
            var server = ServerFactory.Start(_serverInstance,
                async features =>
                {
                    var httpContext = contextFactory.CreateHttpContext(features);
                    httpContext.ApplicationServices = _applicationServices;
                    var requestIdentifier = GetRequestIdentifier(httpContext);
                    contextAccessor.HttpContext = httpContext;
#pragma warning disable 0618
                    if (telemetrySource.IsEnabled("Microsoft.AspNet.Hosting.BeginRequest"))
                    {
                        telemetrySource.WriteTelemetry("Microsoft.AspNet.Hosting.BeginRequest", new { httpContext = httpContext });
                    }
#pragma warning restore 0618
                    try
                    {
                        using (logger.IsEnabled(LogLevel.Critical)
                            ? logger.BeginScope("Request Id: {RequestId}", requestIdentifier) 
                            : null)
                        {
                            await application(httpContext);
                        }
                    }
                    catch (Exception ex)
                    {
#pragma warning disable 0618
                        if (telemetrySource.IsEnabled("Microsoft.AspNet.Hosting.UnhandledException"))
                        {
                            telemetrySource.WriteTelemetry("Microsoft.AspNet.Hosting.UnhandledException", new { httpContext = httpContext, exception = ex });
                        }
#pragma warning restore 0618
                        throw;
                    }
#pragma warning disable 0618
                    if (telemetrySource.IsEnabled("Microsoft.AspNet.Hosting.EndRequest"))
                    {
                        telemetrySource.WriteTelemetry("Microsoft.AspNet.Hosting.EndRequest", new { httpContext = httpContext });
                    }
#pragma warning restore 0618
                });

            _applicationLifetime.NotifyStarted();

            return new Application(ApplicationServices, _serverInstance, new Disposable(() =>
            {
                _applicationLifetime.StopApplication();
                server.Dispose();
                _applicationLifetime.NotifyStopped();
                (_applicationServices as IDisposable)?.Dispose();
            }));
        }

        private void EnsureApplicationServices()
        {
            if (_applicationServices == null)
            {
                EnsureStartup();
                _applicationServiceCollection.AddInstance<IApplicationLifetime>(_applicationLifetime);
                _applicationServices = Startup.ConfigureServicesDelegate(_applicationServiceCollection);
            }
        }

        private void EnsureStartup()
        {
            if (Startup != null)
            {
                return;
            }

            if (StartupType == null)
            {
                var diagnosticTypeMessages = new List<string>();
                StartupType = _startupLoader.FindStartupType(StartupAssemblyName, diagnosticTypeMessages);
                if (StartupType == null)
                {
                    throw new ArgumentException(
                        diagnosticTypeMessages.Aggregate("Failed to find a startup type for the web application.", (a, b) => a + "\r\n" + b),
                        StartupAssemblyName);
                }
            }

            var diagnosticMessages = new List<string>();
            Startup = _startupLoader.LoadMethods(StartupType, diagnosticMessages);
            if (Startup == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find a startup entry point for the web application.", (a, b) => a + "\r\n" + b),
                    StartupAssemblyName);
            }
        }

        private RequestDelegate BuildApplication()
        {
            try
            {
                EnsureApplicationServices();
                EnsureServer();

                var builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
                var builder = builderFactory.CreateBuilder(_serverInstance);
                builder.ApplicationServices = _applicationServices;

                var startupFilters = _applicationServices.GetService<IEnumerable<IStartupFilter>>();
                var configure = Startup.ConfigureDelegate;
                foreach (var filter in startupFilters)
                {
                    configure = filter.Configure(configure);
                }

                configure(builder);

                return builder.Build();
            }
            catch (Exception ex)
            {
                if (!_captureStartupErrors)
                {
                    throw;
                }

                // EnsureApplicationServices may have failed due to a missing or throwing Startup class.
                if (_applicationServices == null)
                {
                    _applicationServices = _applicationServiceCollection.BuildServiceProvider();
                }

                EnsureServer();

                // Write errors to standard out so they can be retrieved when not in development mode.
                Console.Out.WriteLine("Application startup exception: " + ex.ToString());
                var logger = _applicationServices.GetRequiredService<ILogger<HostingEngine>>();
                logger.LogError("Application startup exception", ex);

                // Generate an HTML error page.
                var runtimeEnv = _applicationServices.GetRequiredService<IRuntimeEnvironment>();
                var hostingEnv = _applicationServices.GetRequiredService<IHostingEnvironment>();
                var showDetailedErrors = hostingEnv.IsDevelopment()
                    || string.Equals("true", _config[DetailedErrors], StringComparison.OrdinalIgnoreCase)
                    || string.Equals("1", _config[DetailedErrors], StringComparison.OrdinalIgnoreCase);
                var errorBytes = StartupExceptionPage.GenerateErrorHtml(showDetailedErrors, runtimeEnv, ex);

                return context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.Headers["Cache-Control"] = "private, max-age=0";
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength = errorBytes.Length;
                    return context.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
                };
            }
        }

        private void EnsureServer()
        {
            if (ServerFactory == null)
            {
                // Blow up if we don't have a server set at this point
                if (ServerFactoryLocation == null)
                {
                    throw new InvalidOperationException("IHostingBuilder.UseServer() is required for " + nameof(Start) + "()");
                }

                ServerFactory = _applicationServices.GetRequiredService<IServerLoader>().LoadServerFactory(ServerFactoryLocation);
            }

            if (_serverInstance == null)
            {
                _serverInstance = ServerFactory.Initialize(_config);
                var addresses = _serverInstance?.Get<IServerAddressesFeature>()?.Addresses;
                if (addresses != null && !addresses.IsReadOnly)
                {
                    var port = _config[ServerPort];
                    if (!string.IsNullOrEmpty(port))
                    {
                        addresses.Add("http://localhost:" + port);
                    }

                    // Provide a default address if there aren't any configured.
                    if (addresses.Count == 0)
                    {
                        addresses.Add("http://localhost:5000");
                    }
                }
            }
        }

        private string GetRequestIdentifier(HttpContext httpContext)
        {
            var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
            if (requestIdentifierFeature == null)
            {
                requestIdentifierFeature = new FastHttpRequestIdentifierFeature();
                httpContext.Features.Set(requestIdentifierFeature);
            }

            return requestIdentifierFeature.TraceIdentifier;
        }

        private class Disposable : IDisposable
        {
            private Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, () => { }).Invoke();
            }
        }
    }
}
