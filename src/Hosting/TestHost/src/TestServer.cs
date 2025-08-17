// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// An <see cref="IServer"/> implementation for executing tests.
/// </summary>
public class TestServer : IServer
{
    private readonly IWebHost? _hostInstance;
    private bool _disposed;
    private ApplicationWrapper? _application;

    private static FeatureCollection CreateTestFeatureCollection()
    {
        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        return features;
    }

    /// <summary>
    /// For use with IHostBuilder.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsAccessor"></param>
    public TestServer(IServiceProvider services, IOptions<TestServerOptions> optionsAccessor)
        : this(services, CreateTestFeatureCollection(), optionsAccessor)
    {
    }

    /// <summary>
    /// For use with IHostBuilder.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="featureCollection"></param>
    /// <param name="optionsAccessor"></param>
    public TestServer(IServiceProvider services, IFeatureCollection featureCollection, IOptions<TestServerOptions> optionsAccessor)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Features = featureCollection ?? throw new ArgumentNullException(nameof(featureCollection));
        var options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        AllowSynchronousIO = options.AllowSynchronousIO;
        PreserveExecutionContext = options.PreserveExecutionContext;
        BaseAddress = options.BaseAddress;
    }

    /// <summary>
    /// For use with IHostBuilder.
    /// </summary>
    /// <param name="services"></param>
    public TestServer(IServiceProvider services)
        : this(services, CreateTestFeatureCollection())
    {
    }

    /// <summary>
    /// For use with IHostBuilder.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="featureCollection"></param>
    public TestServer(IServiceProvider services, IFeatureCollection featureCollection)
        : this(services, featureCollection, Options.Create(new TestServerOptions()))
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Features = featureCollection ?? throw new ArgumentNullException(nameof(featureCollection));
    }

    /// <summary>
    /// For use with IWebHostBuilder.
    /// </summary>
    /// <param name="builder"></param>
    public TestServer(IWebHostBuilder builder)
        : this(builder, CreateTestFeatureCollection())
    {
    }

    /// <summary>
    /// For use with IWebHostBuilder.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="featureCollection"></param>
    public TestServer(IWebHostBuilder builder, IFeatureCollection featureCollection)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Features = featureCollection ?? throw new ArgumentNullException(nameof(featureCollection));

        var host = builder.UseServer(this).Build();
        host.StartAsync().GetAwaiter().GetResult();
        _hostInstance = host;

        Services = host.Services;
    }

    /// <summary>
    /// Gets or sets the base address associated with the HttpClient returned by the test server. Defaults to http://localhost/.
    /// </summary>
    public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

    /// <summary>
    /// Gets the <see cref="IWebHost" /> instance associated with the test server.
    /// </summary>
    public IWebHost Host
    {
        get
        {
            return _hostInstance
                ?? throw new InvalidOperationException("The TestServer constructor was not called with a IWebHostBuilder so IWebHost is not available.");
        }
    }

    /// <summary>
    /// Gets the service provider associated with the test server.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the collection of server features associated with the test server.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>. The default value is <see langword="false" />.
    /// </summary>
    public bool AllowSynchronousIO { get; set; }

    /// <summary>
    /// Gets or sets a value that controls if <see cref="ExecutionContext"/> and <see cref="AsyncLocal{T}"/> values are preserved from the client to the server. The default value is <see langword="false" />.
    /// </summary>
    public bool PreserveExecutionContext { get; set; }

    private ApplicationWrapper Application
    {
        get => _application ?? throw new InvalidOperationException("The server has not been started or no web application was configured.");
    }

    private PathString PathBase => BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);

    /// <summary>
    /// Creates a custom <see cref="HttpMessageHandler" /> for processing HTTP requests/responses with the test server.
    /// </summary>
    public HttpMessageHandler CreateHandler()
    {
        return new ClientHandler(PathBase, Application)
        {
            AllowSynchronousIO = AllowSynchronousIO,
            PreserveExecutionContext = PreserveExecutionContext
        };
    }

    /// <summary>
    /// Creates a custom <see cref="HttpMessageHandler" /> for processing HTTP requests/responses with custom configuration with the test server.
    /// </summary>
    public HttpMessageHandler CreateHandler(Action<HttpContext> additionalContextConfiguration)
    {
        return new ClientHandler(PathBase, Application, additionalContextConfiguration)
        {
            AllowSynchronousIO = AllowSynchronousIO,
            PreserveExecutionContext = PreserveExecutionContext
        };
    }

    /// <summary>
    /// Creates a <see cref="HttpClient" /> for processing HTTP requests/responses with the test server.
    /// </summary>
    public HttpClient CreateClient()
    {
        return new HttpClient(CreateHandler())
        {
            BaseAddress = BaseAddress,
            Timeout = TimeSpan.FromSeconds(200),
        };
    }

    /// <summary>
    /// Creates a <see cref="WebSocketClient" /> for interacting with the test server.
    /// </summary>
    public WebSocketClient CreateWebSocketClient()
    {
        var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
        return new WebSocketClient(pathBase, Application) { AllowSynchronousIO = AllowSynchronousIO, PreserveExecutionContext = PreserveExecutionContext };
    }

    /// <summary>
    /// Begins constructing a request message for submission.
    /// </summary>
    /// <param name="path"></param>
    /// <returns><see cref="RequestBuilder"/> to use in constructing additional request details.</returns>
    public RequestBuilder CreateRequest(string path)
    {
        return new RequestBuilder(this, path);
    }

    /// <summary>
    /// Creates, configures, sends, and returns a <see cref="HttpContext"/>. This completes as soon as the response is started.
    /// </summary>
    /// <returns></returns>
    public async Task<HttpContext> SendAsync(Action<HttpContext> configureContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configureContext);

        var builder = new HttpContextBuilder(Application, AllowSynchronousIO, PreserveExecutionContext);
        builder.Configure((context, reader) =>
        {
            var request = context.Request;
            request.Scheme = BaseAddress.Scheme;
            request.Host = HostString.FromUriComponent(BaseAddress);
            if (BaseAddress.IsDefaultPort)
            {
                request.Host = new HostString(request.Host.Host);
            }
            var pathBase = PathString.FromUriComponent(BaseAddress);
            if (pathBase.HasValue && pathBase.Value.EndsWith('/'))
            {
                pathBase = new PathString(pathBase.Value[..^1]); // All but the last character.
            }
            request.PathBase = pathBase;
        });
        builder.Configure((context, reader) => configureContext(context));
        // TODO: Wrap the request body if any?
        return await builder.SendAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Dispose the <see cref="IWebHost" /> object associated with the test server.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _hostInstance?.Dispose();
        }
    }

    Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
    {
        _application = new ApplicationWrapper<TContext>(application, () =>
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        });

        return Task.CompletedTask;
    }

    Task IServer.StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
