// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
public sealed class WebApplication : IHost, IApplicationBuilder, IEndpointRouteBuilder, IAsyncDisposable
{
    internal const string GlobalEndpointRouteBuilderKey = "__GlobalEndpointRouteBuilder";

    private readonly IHost _host;
    private readonly List<EndpointDataSource> _dataSources = new();

    internal WebApplication(IHost host)
    {
        _host = host;
        ApplicationBuilder = new ApplicationBuilder(host.Services);
        Logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Environment.ApplicationName);

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
    /// <param name="args">Command line arguments</param>
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
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateBuilder(string[] args) =>
        new(new() { Args = args });

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationOptions"/> to configure the <see cref="WebApplicationBuilder"/>.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder CreateBuilder(WebApplicationOptions options) =>
        new(options);

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
    public Task RunAsync(string? url = null)
    {
        Listen(url);
        return HostingAbstractionsHostExtensions.RunAsync(this);
    }

    /// <summary>
    /// Runs an application and block the calling thread until host shutdown.
    /// </summary>
    /// <param name="url">The URL to listen to if the server hasn't been configured directly.</param>
    public void Run(string? url = null)
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
}
