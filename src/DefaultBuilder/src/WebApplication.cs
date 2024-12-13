// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// The web application used to configure the HTTP pipeline, and routes.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(WebApplicationDebugView))]
public sealed class WebApplication : IHost, IApplicationBuilder, IEndpointRouteBuilder, IAsyncDisposable
{
    internal const string GlobalEndpointRouteBuilderKey = "__GlobalEndpointRouteBuilder";

    private readonly IHost _host;
    private readonly List<EndpointDataSource> _dataSources = new();

    internal WebApplication(IHost host)
    {
        _host = host;
        ApplicationBuilder = new ApplicationBuilder(host.Services, ServerFeatures);
        Logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Environment.ApplicationName ?? nameof(WebApplication));

        Properties[GlobalEndpointRouteBuilderKey] = this;
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
    /// The list of URLs that the HTTP server is bound to.
    /// </summary>
    public ICollection<string> Urls => ServerFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

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

    internal ApplicationBuilder ApplicationBuilder { get; }

    IServiceProvider IEndpointRouteBuilder.ServiceProvider => Services;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplication"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static WebApplication Create(string[]? args = null) =>
        new WebApplicationBuilder(new() { Args = args }).Build();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateBuilder() =>
        new(new());

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with minimal defaults.
    /// </summary>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateSlimBuilder() =>
        new(new(), slim: true);

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateBuilder(string[] args) =>
        new(new() { Args = args });

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with minimal defaults.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateSlimBuilder(string[] args) =>
        new(new() { Args = args }, slim: true);

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationOptions"/> to configure the <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateBuilder(WebApplicationOptions options) =>
        new(options);

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with minimal defaults.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationOptions"/> to configure the <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateSlimBuilder(WebApplicationOptions options) =>
        new(options, slim: true);

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with no defaults.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationOptions"/> to configure the <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateEmptyBuilder(WebApplicationOptions options) =>
        new(options, slim: false, empty: true);

    /// <summary>
    /// Start the application.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A <see cref="Task"/> that represents the startup of the <see cref="WebApplication"/>.
    /// Successful completion indicates the HTTP server is ready to accept new requests.
    /// </returns>
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _host.StartAsync(cancellationToken);

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A <see cref="Task"/> that represents the shutdown of the <see cref="WebApplication"/>.
    /// Successful completion indicates that all the HTTP server has stopped.
    /// </returns>
    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _host.StopAsync(cancellationToken);

    /// <summary>
    /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
    /// </summary>
    /// <param name="url">The URL to listen to if the server hasn't been configured directly.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the entire runtime of the <see cref="WebApplication"/> from startup to shutdown.
    /// </returns>
    public Task RunAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? url = null)
    {
        Listen(url);
        return HostingAbstractionsHostExtensions.RunAsync(this);
    }

    /// <summary>
    /// Runs an application and blocks the calling thread until host shutdown.
    /// </summary>
    /// <param name="url">The URL to listen to if the server hasn't been configured directly.</param>
    public void Run([StringSyntax(StringSyntaxAttribute.Uri)] string? url = null)
    {
        Listen(url);
        HostingAbstractionsHostExtensions.Run(this);
    }

    /// <summary>
    /// Disposes the application.
    /// </summary>
    void IDisposable.Dispose() => _host.Dispose();

    /// <summary>
    /// Disposes the application.
    /// </summary>
    public ValueTask DisposeAsync() => ((IAsyncDisposable)_host).DisposeAsync();

    internal RequestDelegate BuildRequestDelegate() => ApplicationBuilder.Build();
    RequestDelegate IApplicationBuilder.Build() => BuildRequestDelegate();

    // REVIEW: Should this be wrapping another type?
    IApplicationBuilder IApplicationBuilder.New()
    {
        var newBuilder = ApplicationBuilder.New();
        // Remove the route builder so branched pipelines have their own routing world
        newBuilder.Properties.Remove(GlobalEndpointRouteBuilderKey);
        return newBuilder;
    }

    /// <summary>
    /// Adds the middleware to the application request pipeline.
    /// </summary>
    /// <param name="middleware">The middleware.</param>
    /// <returns>An instance of <see cref="IApplicationBuilder"/> after the operation has completed.</returns>
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        ApplicationBuilder.Use(middleware);
        return this;
    }

    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => ((IApplicationBuilder)this).New();

    private void Listen(string? url)
    {
        if (url is null)
        {
            return;
        }

        var addresses = ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null)
        {
            throw new InvalidOperationException($"Changing the URL is not supported because no valid {nameof(IServerAddressesFeature)} was found.");
        }
        if (addresses.IsReadOnly)
        {
            throw new InvalidOperationException($"Changing the URL is not supported because {nameof(IServerAddressesFeature.Addresses)} {nameof(ICollection<string>.IsReadOnly)}.");
        }

        addresses.Clear();
        addresses.Add(url);
    }

    private string DebuggerToString()
    {
        return $@"ApplicationName = ""{Environment.ApplicationName}"", IsRunning = {(IsRunning ? "true" : "false")}";
    }

    // Web app is running if the app has been started and hasn't been stopped.
    private bool IsRunning => Lifetime.ApplicationStarted.IsCancellationRequested && !Lifetime.ApplicationStopped.IsCancellationRequested;

    internal sealed class WebApplicationDebugView(WebApplication webApplication)
    {
        private readonly WebApplication _webApplication = webApplication;

        public IServiceProvider Services => _webApplication.Services;
        public IConfiguration Configuration => _webApplication.Configuration;
        public IWebHostEnvironment Environment => _webApplication.Environment;
        public IHostApplicationLifetime Lifetime => _webApplication.Lifetime;
        public ILogger Logger => _webApplication.Logger;
        public string Urls => string.Join(", ", _webApplication.Urls);
        public IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                var dataSource = _webApplication.Services.GetRequiredService<EndpointDataSource>();
                if (dataSource is CompositeEndpointDataSource compositeEndpointDataSource)
                {
                    // The web app's data sources aren't registered until the routing middleware is. That often happens when the app is run.
                    // We want endpoints to be available in the debug view before the app starts. Test if all the web app's the data sources are registered.
                    if (compositeEndpointDataSource.DataSources.Intersect(_webApplication.DataSources).Count() == _webApplication.DataSources.Count)
                    {
                        // Data sources are centrally registered.
                        return dataSource.Endpoints;
                    }
                    else
                    {
                        // Fallback to just the web app's data sources to support debugging before the web app starts.
                        return new CompositeEndpointDataSource(_webApplication.DataSources).Endpoints;
                    }
                }

                return dataSource.Endpoints;
            }
        }
        public bool IsRunning => _webApplication.IsRunning;
        public IList<string>? Middleware
        {
            get
            {
                if (_webApplication.Properties.TryGetValue("__MiddlewareDescriptions", out var value) &&
                    value is IList<string> descriptions)
                {
                    return descriptions;
                }

                throw new NotSupportedException("Unable to get configured middleware.");
            }
        }
    }
}
