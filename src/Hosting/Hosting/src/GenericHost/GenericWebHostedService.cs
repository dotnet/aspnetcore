// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.StackTrace.Sources;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Hosting
{
    internal class GenericWebHostService : IHostedService
    {
        public GenericWebHostService(IOptions<GenericWebHostServiceOptions> options,
                                     IServer server,
                                     ILoggerFactory loggerFactory,
                                     DiagnosticListener diagnosticListener,
                                     IHttpContextFactory httpContextFactory,
                                     IApplicationBuilderFactory applicationBuilderFactory,
                                     IEnumerable<IStartupFilter> startupFilters,
                                     IConfiguration configuration,
                                     IWebHostEnvironment hostingEnvironment)
        {
            Options = options.Value;
            Server = server;
            Logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics");
            LifetimeLogger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
            DiagnosticListener = diagnosticListener;
            HttpContextFactory = httpContextFactory;
            ApplicationBuilderFactory = applicationBuilderFactory;
            StartupFilters = startupFilters;
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public GenericWebHostServiceOptions Options { get; }
        public IServer Server { get; }
        public ILogger Logger { get; }
        // Only for high level lifetime events
        public ILogger LifetimeLogger { get; }
        public DiagnosticListener DiagnosticListener { get; }
        public IHttpContextFactory HttpContextFactory { get; }
        public IApplicationBuilderFactory ApplicationBuilderFactory { get; }
        public IEnumerable<IStartupFilter> StartupFilters { get; }
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            HostingEventSource.Log.HostStart();

            var serverAddressesFeature = Server.Features?.Get<IServerAddressesFeature>();
            var addresses = serverAddressesFeature?.Addresses;
            if (addresses != null && !addresses.IsReadOnly && addresses.Count == 0)
            {
                var urls = Configuration[WebHostDefaults.ServerUrlsKey];
                if (!string.IsNullOrEmpty(urls))
                {
                    serverAddressesFeature.PreferHostingUrls = WebHostUtilities.ParseBool(Configuration, WebHostDefaults.PreferHostingUrlsKey);

                    foreach (var value in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        addresses.Add(value);
                    }
                }
            }

            RequestDelegate application = null;

            try
            {
                Action<IApplicationBuilder> configure = Options.ConfigureApplication;

                if (configure == null)
                {
                    throw new InvalidOperationException($"No application configured. Please specify an application via IWebHostBuilder.UseStartup, IWebHostBuilder.Configure, or specifying the startup assembly via {nameof(WebHostDefaults.StartupAssemblyKey)} in the web host configuration.");
                }

                var builder = ApplicationBuilderFactory.CreateBuilder(Server.Features);

                foreach (var filter in StartupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                configure(builder);

                // Build the request pipeline
                application = builder.Build();
            }
            catch (Exception ex)
            {
                Logger.ApplicationError(ex);

                if (!Options.WebHostOptions.CaptureStartupErrors)
                {
                    throw;
                }

                application = BuildErrorPageApplication(ex);
            }

            var httpApplication = new HostingApplication(application, Logger, DiagnosticListener, HttpContextFactory);

            await Server.StartAsync(httpApplication, cancellationToken);

            if (addresses != null)
            {
                foreach (var address in addresses)
                {
                    LifetimeLogger.LogInformation("Now listening on: {address}", address);
                }
            }

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var assembly in Options.WebHostOptions.GetFinalHostingStartupAssemblies())
                {
                    Logger.LogDebug("Loaded hosting startup assembly {assemblyName}", assembly);
                }
            }

            if (Options.HostingStartupExceptions != null)
            {
                foreach (var exception in Options.HostingStartupExceptions.InnerExceptions)
                {
                    Logger.HostingStartupAssemblyError(exception);
                }
            }
        }

        private RequestDelegate BuildErrorPageApplication(Exception exception)
        {
            if (exception is TargetInvocationException tae)
            {
                exception = tae.InnerException;
            }

            var showDetailedErrors = HostingEnvironment.IsDevelopment() || Options.WebHostOptions.DetailedErrors;

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
                    HostingEnvironment.ContentRootFileProvider,
                    Logger,
                    sourceCodeLineCount: 6);

                model.ErrorDetails = exceptionDetailProvider.GetDetails(exception);
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
                context.Response.ContentType = "text/html; charset=utf-8";
                return errorPage.ExecuteAsync(context);
            };
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Server.StopAsync(cancellationToken);
            }
            finally
            {
                HostingEventSource.Log.HostStop();
            }
        }
    }
}
