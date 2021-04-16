// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// The web application used to configure the http pipeline, and routes.
    /// </summary>
    public sealed class WebApplication : IHost, IDisposable, IApplicationBuilder, IEndpointRouteBuilder, IAsyncDisposable
    {
        internal const string EndpointRouteBuilder = "__EndpointRouteBuilder";

        private readonly IHost _host;
        private readonly List<EndpointDataSource> _dataSources = new();

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
        public IHostApplicationLifetime Lifetime => _host.Services.GetRequiredService<IHostApplicationLifetime>();

        /// <summary>
        /// The default logger for the application.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// The list of addresses that the HTTP server is bound to.
        /// </summary>
        public IEnumerable<string>? Addresses => ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;

        IServiceProvider IApplicationBuilder.ApplicationServices
        {
            get => ApplicationBuilder.ApplicationServices;
            set => ApplicationBuilder.ApplicationServices = value;
        }

        internal IFeatureCollection ServerFeatures => _host.Services.GetRequiredService<IServer>().Features;
        IFeatureCollection IApplicationBuilder.ServerFeatures => ServerFeatures;

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
        /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <returns>The <see cref="WebApplicationBuilder"/></returns>
        public static WebApplicationBuilder CreateBuilder() =>
            // The assumption here is that this API is called by the application directly
            // this might give a better approximation of the default application name
            new WebApplicationBuilder(Assembly.GetCallingAssembly());

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>The <see cref="WebApplicationBuilder"/></returns>
        public static WebApplicationBuilder CreateBuilder(string[] args) =>
            new WebApplicationBuilder(Assembly.GetCallingAssembly(), args);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplication"/> class with pre-configured defaults.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>The <see cref="WebApplication"/></returns>
        public static WebApplication Create(string[] args) =>
            new WebApplicationBuilder(Assembly.GetCallingAssembly(), args).Build();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplication"/> class with pre-configured defaults.
        /// </summary>
        /// <returns>The <see cref="WebApplication"/></returns>
        public static WebApplication Create()
            => new WebApplicationBuilder(Assembly.GetCallingAssembly()).Build();

        /// <summary>
        /// Start the application.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken = default) =>
            _host.StartAsync(cancellationToken);

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken = default) =>
            _host.StopAsync(cancellationToken);

        /// <summary>
        /// Disposes the application.
        /// </summary>
        void IDisposable.Dispose() => _host.Dispose();

        /// <summary>
        /// Disposes the application.
        /// </summary>
        public ValueTask DisposeAsync() => ((IAsyncDisposable)_host).DisposeAsync();

        internal RequestDelegate Build() => ApplicationBuilder.Build();
        RequestDelegate IApplicationBuilder.Build() => Build();

        // REVIEW: Should this be wrapping another type?
        IApplicationBuilder IApplicationBuilder.New() => ApplicationBuilder.New();

        IApplicationBuilder IApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            ApplicationBuilder.Use(middleware);
            return this;
        }

        IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => ApplicationBuilder.New();

        /// <summary>
        /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <returns>A <see cref="Task"/>that represents the asynchronous operation.</returns>
        // REVIEW: We cannot use a default param for the CT because of the params urls overload. Are we okay with this?
        public Task RunAsync() => RunAsync(CancellationToken.None);

        /// <summary>
        /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="cancellationToken">The token to trigger shutdown.</param>
        /// <returns>A <see cref="Task"/>that represents the asynchronous operation.</returns>
        public Task RunAsync(CancellationToken cancellationToken) =>
            HostingAbstractionsHostExtensions.RunAsync(this, cancellationToken);

        /// <summary>
        /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="urls">A set of urls to listen to if the server hasn't been configured directly.</param>
        /// <returns>A <see cref="Task"/>that represents the asynchronous operation.</returns>
        public Task RunAsync(params string[] urls)
        {
            Listen(urls);
            return HostingAbstractionsHostExtensions.RunAsync(this);
        }

        /// <summary>
        /// Runs an application and block the calling thread until host shutdown.
        /// </summary>
        public void Run() =>
            HostingAbstractionsHostExtensions.Run(this);

        /// <summary>
        /// Sets the URLs the web server will listen on.
        /// </summary>
        /// <param name="urls">A set of urls to listen to if the server hasn't been configured directly.</param>
        private void Listen(params string[] urls)
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
    }
}
