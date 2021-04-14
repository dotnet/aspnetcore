// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

// REVIEW: Or just "Microsoft.AspNetCore" like WebHost?
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// The web application used to configure the http pipeline, and routes.
    /// </summary>
    public class WebApplication : IHost, IDisposable, IApplicationBuilder, IEndpointRouteBuilder
    {
        internal const string EndpointRouteBuilder = "__EndpointRouteBuilder";

        private readonly IHost _host;
        private readonly List<EndpointDataSource> _dataSources = new List<EndpointDataSource>();

        internal WebApplication(IHost host)
        {
            _host = host;
            ApplicationBuilder = new ApplicationBuilder(host.Services);
            Logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Environment.ApplicationName);
        }

        /// <summary>
        /// The application's configured services.
        /// </summary>
        public IServiceProvider Services => _host.Services;

        /// <summary>
        /// The application's configured <see cref="IConfiguration"/>.
        /// </summary>
        public IConfiguration Configuration => _host.Services.GetRequiredService<IConfiguration>();

        /// <summary>
        /// The application's configured <see cref="IWebHostEnvironment"/>.
        /// </summary>
        public IWebHostEnvironment Environment => _host.Services.GetRequiredService<IWebHostEnvironment>();

        /// <summary>
        /// Allows consumers to be notified of application lifetime events.
        /// </summary>
        public IHostApplicationLifetime ApplicationLifetime => _host.Services.GetRequiredService<IHostApplicationLifetime>();

        /// <summary>
        /// The logger factory for the application.
        /// </summary>
        public ILoggerFactory LoggerFactory => _host.Services.GetRequiredService<ILoggerFactory>();

        /// <summary>
        /// The default logger for the application.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// The list of addresses that the HTTP server is bound to.
        /// </summary>
        public IEnumerable<string>? Addresses => ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;

        /// <summary>
        /// A collection of HTTP features of the server.
        /// </summary>
        public IFeatureCollection ServerFeatures => _host.Services.GetRequiredService<IServer>().Features;

        IServiceProvider IApplicationBuilder.ApplicationServices { get => ApplicationBuilder.ApplicationServices; set => ApplicationBuilder.ApplicationServices = value; }

        internal IDictionary<string, object?> Properties => ApplicationBuilder.Properties;
        IDictionary<string, object?> IApplicationBuilder.Properties => Properties;

        internal ICollection<EndpointDataSource> DataSources => _dataSources;
        ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => DataSources;

        internal IEndpointRouteBuilder RouteBuilder
        {
            get
            {
                Properties.TryGetValue(EndpointRouteBuilder, out var value);
                return (IEndpointRouteBuilder)value!;
            }
        }

        internal ApplicationBuilder ApplicationBuilder { get; }

        IServiceProvider IEndpointRouteBuilder.ServiceProvider => Services;

        /// <summary>
        /// Sets the URLs the web server will listen on.
        /// </summary>
        /// <param name="urls">A set of urls.</param>
        public void Listen(params string[] urls)
        {
            var addresses = ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
            if (addresses is null || addresses.IsReadOnly)
            {
                throw new NotSupportedException("Changing the URL isn't supported.");
            }

            addresses.Clear();
            foreach (var u in urls)
            {
                addresses.Add(u);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <returns>The <see cref="WebApplicationBuilder"/></returns>
        public static WebApplicationBuilder CreateBuilder()
        {
            // The assumption here is that this API is called by the application directly
            // this might give a better approximation of the default application name
            return new WebApplicationBuilder(
                Assembly.GetCallingAssembly(),
                builder => ConfigureBuilder(builder, args: null));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>The <see cref="WebApplicationBuilder"/></returns>
        public static WebApplicationBuilder CreateBuilder(string[] args)
        {
            return new WebApplicationBuilder(
                Assembly.GetCallingAssembly(),
                builder => ConfigureBuilder(builder, args));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplication"/> class with pre-configured defaults.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>The <see cref="WebApplication"/></returns>
        public static WebApplication Create(string[] args)
        {
            return new WebApplicationBuilder(
                Assembly.GetCallingAssembly(),
                builder => ConfigureBuilder(builder, args)).Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplication"/> class with pre-configured defaults.
        /// </summary>
        /// <returns>The <see cref="WebApplication"/></returns>
        public static WebApplication Create()
        {
            return new WebApplicationBuilder(
                Assembly.GetCallingAssembly(),
                builder => ConfigureBuilder(builder, args: null)).Build();
        }

        /// <summary>
        /// Start the application.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return _host.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return _host.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Disposes the application.
        /// </summary>
        public void Dispose()
        {
            _host.Dispose();
        }

        internal RequestDelegate Build() => ApplicationBuilder.Build();
        RequestDelegate IApplicationBuilder.Build() => Build();

        IApplicationBuilder IApplicationBuilder.New()
        {
            // REVIEW: Should this be wrapping another type?
            return ApplicationBuilder.New();
        }

        IApplicationBuilder IApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            ApplicationBuilder.Use(middleware);
            return this;
        }

        IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => ApplicationBuilder.New();

        /// <summary>
        /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="cancellationToken">The token to trigger shutdown.</param>
        /// <returns>A <see cref="Task"/>that represents the asynchronous operation.</returns>
        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            return HostingAbstractionsHostExtensions.RunAsync(this, cancellationToken);
        }

        /// <summary>
        /// Runs an application and block the calling thread until host shutdown.
        /// </summary>
        public void Run()
        {
            HostingAbstractionsHostExtensions.Run(this);
        }

        private static void ConfigureBuilder(IHostBuilder builder, string[]? args)
        {
            // Keep in sync with this Host.CreateDefaultBuilder https://github.com/dotnet/extensions/blob/cb60ad143f61f0d96b0860895065351e86f79a10/src/Hosting/Hosting/src/Host.cs#L56

            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables(prefix: "DOTNET_");
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                var isWindows = OperatingSystem.IsWindows();

                // IMPORTANT: This needs to be added *before* configuration is loaded, this lets
                // the defaults be overridden by the configuration.
                if (isWindows)
                {
                    // Default the EventLogLoggerProvider to warning or above
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
                }

                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();

                if (isWindows)
                {
                    // Add the EventLogLoggerProvider on windows machines
                    logging.AddEventLog();
                }
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });
        }
    }
}
