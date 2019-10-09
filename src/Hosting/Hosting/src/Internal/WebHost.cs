// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.StackTrace.Sources;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Hosting
{
    internal class WebHost : IWebHost, IAsyncDisposable
    {
        private static readonly string DeprecatedServerUrlsKey = "server.urls";

        private readonly IServiceCollection _applicationServiceCollection;
        private IStartup _startup;
        private ApplicationLifetime _applicationLifetime;
        private HostedServiceExecutor _hostedServiceExecutor;

        private readonly IServiceProvider _hostingServiceProvider;
        private readonly WebHostOptions _options;
        private readonly IConfiguration _config;
        private readonly AggregateException _hostingStartupErrors;

        private IServiceProvider _applicationServices;
        private ExceptionDispatchInfo _applicationServicesException;
        private ILogger _logger =  NullLogger.Instance;

        private bool _stopped;
        private bool _startedServer;

        // Used for testing only
        internal WebHostOptions Options => _options;

        private IServer Server { get; set; }

        public WebHost(
            IServiceCollection appServices,
            IServiceProvider hostingServiceProvider,
            WebHostOptions options,
            IConfiguration config,
            AggregateException hostingStartupErrors)
        {
            if (appServices == null)
            {
                throw new ArgumentNullException(nameof(appServices));
            }

            if (hostingServiceProvider == null)
            {
                throw new ArgumentNullException(nameof(hostingServiceProvider));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _hostingStartupErrors = hostingStartupErrors;
            _options = options;
            _applicationServiceCollection = appServices;
            _hostingServiceProvider = hostingServiceProvider;
            _applicationServiceCollection.AddSingleton<ApplicationLifetime>();
            // There's no way to to register multiple service types per definition. See https://github.com/aspnet/DependencyInjection/issues/360
            _applicationServiceCollection.AddSingleton(services
                => services.GetService<ApplicationLifetime>() as IHostApplicationLifetime);
#pragma warning disable CS0618 // Type or member is obsolete
            _applicationServiceCollection.AddSingleton(services
                => services.GetService<ApplicationLifetime>() as AspNetCore.Hosting.IApplicationLifetime);
            _applicationServiceCollection.AddSingleton(services
                => services.GetService<ApplicationLifetime>() as Extensions.Hosting.IApplicationLifetime);
#pragma warning restore CS0618 // Type or member is obsolete
            _applicationServiceCollection.AddSingleton<HostedServiceExecutor>();
        }

        public IServiceProvider Services
        {
            get
            {
                return _applicationServices;
            }
        }

        public IFeatureCollection ServerFeatures
        {
            get
            {
                EnsureServer();
                return Server?.Features;
            }
        }

        // Called immediately after the constructor so the properties can rely on it.
        public void Initialize()
        {
            try
            {
                EnsureApplicationServices();
            }
            catch (Exception ex)
            {
                // EnsureApplicationServices may have failed due to a missing or throwing Startup class.
                if (_applicationServices == null)
                {
                    _applicationServices = _applicationServiceCollection.BuildServiceProvider();
                }

                if (!_options.CaptureStartupErrors)
                {
                    throw;
                }

                _applicationServicesException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken = default)
        {
            HostingEventSource.Log.HostStart();
            _logger = _applicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics");
            _logger.Starting();

            var application = BuildApplication();

            _applicationLifetime = _applicationServices.GetRequiredService<ApplicationLifetime>();
            _hostedServiceExecutor = _applicationServices.GetRequiredService<HostedServiceExecutor>();

            // Fire IHostedService.Start
            await _hostedServiceExecutor.StartAsync(cancellationToken).ConfigureAwait(false);

            var diagnosticSource = _applicationServices.GetRequiredService<DiagnosticListener>();
            var httpContextFactory = _applicationServices.GetRequiredService<IHttpContextFactory>();
            var hostingApp = new HostingApplication(application, _logger, diagnosticSource, httpContextFactory);
            await Server.StartAsync(hostingApp, cancellationToken).ConfigureAwait(false);
            _startedServer = true;

            // Fire IApplicationLifetime.Started
            _applicationLifetime?.NotifyStarted();


            _logger.Started();

            // Log the fact that we did load hosting startup assemblies.
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var assembly in _options.GetFinalHostingStartupAssemblies())
                {
                    _logger.LogDebug("Loaded hosting startup assembly {assemblyName}", assembly);
                }
            }

            if (_hostingStartupErrors != null)
            {
                foreach (var exception in _hostingStartupErrors.InnerExceptions)
                {
                    _logger.HostingStartupAssemblyError(exception);
                }
            }
        }

        private void EnsureApplicationServices()
        {
            if (_applicationServices == null)
            {
                EnsureStartup();
                _applicationServices = _startup.ConfigureServices(_applicationServiceCollection);
            }
        }

        private void EnsureStartup()
        {
            if (_startup != null)
            {
                return;
            }

            _startup = _hostingServiceProvider.GetService<IStartup>();

            if (_startup == null)
            {
                throw new InvalidOperationException($"No application configured. Please specify startup via IWebHostBuilder.UseStartup, IWebHostBuilder.Configure, injecting {nameof(IStartup)} or specifying the startup assembly via {nameof(WebHostDefaults.StartupAssemblyKey)} in the web host configuration.");
            }
        }

        private RequestDelegate BuildApplication()
        {
            try
            {
                _applicationServicesException?.Throw();
                EnsureServer();

                var builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
                var builder = builderFactory.CreateBuilder(Server.Features);
                builder.ApplicationServices = _applicationServices;

                var startupFilters = _applicationServices.GetService<IEnumerable<IStartupFilter>>();
                Action<IApplicationBuilder> configure = _startup.Configure;
                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                configure(builder);

                return builder.Build();
            }
            catch (Exception ex)
            {
                if (!_options.SuppressStatusMessages)
                {
                    // Write errors to standard out so they can be retrieved when not in development mode.
                    Console.WriteLine("Application startup exception: " + ex.ToString());
                }
                var logger = _applicationServices.GetRequiredService<ILogger<WebHost>>();
                logger.ApplicationError(ex);

                if (!_options.CaptureStartupErrors)
                {
                    throw;
                }

                EnsureServer();

                // Generate an HTML error page.
                var hostingEnv = _applicationServices.GetRequiredService<IHostEnvironment>();
                var showDetailedErrors = hostingEnv.IsDevelopment() || _options.DetailedErrors;

                var model = new ErrorPageModel
                {
                    RuntimeDisplayName = RuntimeInformation.FrameworkDescription
                };
                var systemRuntimeAssembly = typeof(System.ComponentModel.DefaultValueAttribute).GetTypeInfo().Assembly;
                var assemblyVersion = new AssemblyName(systemRuntimeAssembly.FullName).Version.ToString();
                var clrVersion = assemblyVersion;
                model.RuntimeArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
                var currentAssembly = typeof(ErrorPage).GetTypeInfo().Assembly;
                model.CurrentAssemblyVesion = currentAssembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
                model.ClrVersion = clrVersion;
                model.OperatingSystemDescription = RuntimeInformation.OSDescription;
                model.ShowRuntimeDetails = showDetailedErrors;

                if (showDetailedErrors)
                {
                    var exceptionDetailProvider = new ExceptionDetailsProvider(
                        hostingEnv.ContentRootFileProvider,
                        logger,
                        sourceCodeLineCount: 6);

                    model.ErrorDetails = exceptionDetailProvider.GetDetails(ex);
                }
                else
                {
                    model.ErrorDetails = new ExceptionDetails[0];
                }

                var errorPage = new ErrorPage(model);
                return context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                    return errorPage.ExecuteAsync(context);
                };
            }
        }

        private void EnsureServer()
        {
            if (Server == null)
            {
                Server = _applicationServices.GetRequiredService<IServer>();

                var serverAddressesFeature = Server.Features?.Get<IServerAddressesFeature>();
                var addresses = serverAddressesFeature?.Addresses;
                if (addresses != null && !addresses.IsReadOnly && addresses.Count == 0)
                {
                    var urls = _config[WebHostDefaults.ServerUrlsKey] ?? _config[DeprecatedServerUrlsKey];
                    if (!string.IsNullOrEmpty(urls))
                    {
                        serverAddressesFeature.PreferHostingUrls = WebHostUtilities.ParseBool(_config, WebHostDefaults.PreferHostingUrlsKey);

                        foreach (var value in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            addresses.Add(value);
                        }
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_stopped)
            {
                return;
            }
            _stopped = true;

            _logger.Shutdown();

            var timeoutToken = new CancellationTokenSource(Options.ShutdownTimeout).Token;
            if (!cancellationToken.CanBeCanceled)
            {
                cancellationToken = timeoutToken;
            }
            else
            {
                cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
            }

            // Fire IApplicationLifetime.Stopping
            _applicationLifetime?.StopApplication();

            if (Server != null && _startedServer)
            {
                await Server.StopAsync(cancellationToken).ConfigureAwait(false);
            }

            // Fire the IHostedService.Stop
            if (_hostedServiceExecutor != null)
            {
                await _hostedServiceExecutor.StopAsync(cancellationToken).ConfigureAwait(false);
            }

            // Fire IApplicationLifetime.Stopped
            _applicationLifetime?.NotifyStopped();

            HostingEventSource.Log.HostStop();
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_stopped)
            {
                try
                {
                    await StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ServerShutdownException(ex);
                }
            }

            await DisposeServiceProviderAsync(_applicationServices).ConfigureAwait(false);
            await DisposeServiceProviderAsync(_hostingServiceProvider).ConfigureAwait(false);
        }

        private async ValueTask DisposeServiceProviderAsync(IServiceProvider serviceProvider)
        {
            switch (serviceProvider)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}
