// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

internal sealed partial class GenericWebHostService : IHostedService
{
    public GenericWebHostService(IOptions<GenericWebHostServiceOptions> options,
                                 IServer server,
                                 ILoggerFactory loggerFactory,
                                 DiagnosticListener diagnosticListener,
                                 ActivitySource activitySource,
                                 DistributedContextPropagator propagator,
                                 IHttpContextFactory httpContextFactory,
                                 IApplicationBuilderFactory applicationBuilderFactory,
                                 IEnumerable<IStartupFilter> startupFilters,
                                 IConfiguration configuration,
                                 IWebHostEnvironment hostingEnvironment,
                                 HostingMetrics hostingMetrics)
    {
        Options = options.Value;
        Server = server;
        Logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics");
        LifetimeLogger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        DiagnosticListener = diagnosticListener;
        ActivitySource = activitySource;
        Propagator = propagator;
        HttpContextFactory = httpContextFactory;
        ApplicationBuilderFactory = applicationBuilderFactory;
        StartupFilters = startupFilters;
        Configuration = configuration;
        HostingEnvironment = hostingEnvironment;
        HostingMetrics = hostingMetrics;
    }

    public GenericWebHostServiceOptions Options { get; }
    public IServer Server { get; }
    public ILogger Logger { get; }
    // Only for high level lifetime events
    public ILogger LifetimeLogger { get; }
    public DiagnosticListener DiagnosticListener { get; }
    public ActivitySource ActivitySource { get; }
    public DistributedContextPropagator Propagator { get; }
    public IHttpContextFactory HttpContextFactory { get; }
    public IApplicationBuilderFactory ApplicationBuilderFactory { get; }
    public IEnumerable<IStartupFilter> StartupFilters { get; }
    public IConfiguration Configuration { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    public HostingMetrics HostingMetrics { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HostingEventSource.Log.HostStart();

        var serverAddressesFeature = Server.Features.Get<IServerAddressesFeature>();
        var addresses = serverAddressesFeature?.Addresses;
        if (addresses != null && !addresses.IsReadOnly && addresses.Count == 0)
        {
            // We support reading "urls" from app configuration
            var urls = Configuration[WebHostDefaults.ServerUrlsKey];

            // But fall back to host settings
            if (string.IsNullOrEmpty(urls))
            {
                urls = Options.WebHostOptions.ServerUrls;
            }

            var httpPorts = Configuration[WebHostDefaults.HttpPortsKey] ?? string.Empty;
            var httpsPorts = Configuration[WebHostDefaults.HttpsPortsKey] ?? string.Empty;
            if (string.IsNullOrEmpty(urls))
            {
                // HTTP_PORTS and HTTPS_PORTS, these are lower priority than Urls.
                static string ExpandPorts(string ports, string scheme)
                {
                    return string.Join(';',
                        ports.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(port => $"{scheme}://*:{port}"));
                }

                var httpUrls = ExpandPorts(httpPorts, Uri.UriSchemeHttp);
                var httpsUrls = ExpandPorts(httpsPorts, Uri.UriSchemeHttps);
                urls = $"{httpUrls};{httpsUrls}";
            }
            else if (!string.IsNullOrEmpty(httpPorts) || !string.IsNullOrEmpty(httpsPorts))
            {
                Logger.PortsOverridenByUrls(httpPorts, httpsPorts, urls);
            }

            if (!string.IsNullOrEmpty(urls))
            {
                // We support reading "preferHostingUrls" from app configuration
                var preferHostingUrlsConfig = Configuration[WebHostDefaults.PreferHostingUrlsKey];

                // But fall back to host settings
                if (!string.IsNullOrEmpty(preferHostingUrlsConfig))
                {
                    serverAddressesFeature!.PreferHostingUrls = WebHostUtilities.ParseBool(preferHostingUrlsConfig);
                }
                else
                {
                    serverAddressesFeature!.PreferHostingUrls = Options.WebHostOptions.PreferHostingUrls;
                }

                foreach (var value in urls.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    addresses.Add(value);
                }
            }
        }

        RequestDelegate? application = null;

        try
        {
            var configure = Options.ConfigureApplication;

            if (configure == null)
            {
                throw new InvalidOperationException($"No application configured. Please specify an application via IWebHostBuilder.UseStartup, IWebHostBuilder.Configure, or specifying the startup assembly via {nameof(WebHostDefaults.StartupAssemblyKey)} in the web host configuration.");
            }

            var builder = ApplicationBuilderFactory.CreateBuilder(Server.Features);

            foreach (var filter in Enumerable.Reverse(StartupFilters))
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

            var showDetailedErrors = HostingEnvironment.IsDevelopment() || Options.WebHostOptions.DetailedErrors;

            application = ErrorPageBuilder.BuildErrorPageApplication(HostingEnvironment.ContentRootFileProvider, Logger, showDetailedErrors, ex);
        }

        var httpApplication = new HostingApplication(application, Logger, DiagnosticListener, ActivitySource, Propagator, HttpContextFactory, HostingEventSource.Log, HostingMetrics);

        await Server.StartAsync(httpApplication, cancellationToken);
        HostingEventSource.Log.ServerReady();

        if (addresses != null)
        {
            foreach (var address in addresses)
            {
                Log.ListeningOnAddress(LifetimeLogger, address);
            }
        }

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var assembly in Options.WebHostOptions.GetFinalHostingStartupAssemblies())
            {
                Log.StartupAssemblyLoaded(Logger, assembly);
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

    private static partial class Log
    {
        [LoggerMessage(14, LogLevel.Information,
            "Now listening on: {address}",
            EventName = "ListeningOnAddress")]
        public static partial void ListeningOnAddress(ILogger logger, string address);

        [LoggerMessage(13, LogLevel.Debug,
            "Loaded hosting startup assembly {assemblyName}",
            EventName = "HostingStartupAssemblyLoaded",
            SkipEnabledCheck = true)]
        public static partial void StartupAssemblyLoaded(ILogger logger, string assemblyName);
    }
}
