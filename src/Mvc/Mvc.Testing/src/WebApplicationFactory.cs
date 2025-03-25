// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Factory for bootstrapping an application in memory for functional end to end tests.
/// </summary>
/// <typeparam name="TEntryPoint">A type in the entry point assembly of the application.
/// Typically the Startup or Program classes can be used.</typeparam>
public partial class WebApplicationFactory<TEntryPoint> : IDisposable, IAsyncDisposable where TEntryPoint : class
{
    private bool _disposed;
    private bool _disposedAsync;

    private bool _useKestrel;
    private int? _kestrelPort;
    private Action<KestrelServerOptions>? _configureKestrelOptions;

    private TestServer? _server;
    private IHost? _host;
    private Action<IWebHostBuilder> _configuration;
    private IWebHost? _webHost;
    private Uri? _webHostAddress;
    private readonly List<HttpClient> _clients = new();
    private readonly List<WebApplicationFactory<TEntryPoint>> _derivedFactories = new();

    /// <summary>
    /// <para>
    /// Creates an instance of <see cref="WebApplicationFactory{TEntryPoint}"/>. This factory can be used to
    /// create a <see cref="TestServer"/> instance using the MVC application defined by <typeparamref name="TEntryPoint"/>
    /// and one or more <see cref="HttpClient"/> instances used to send <see cref="HttpRequestMessage"/> to the <see cref="TestServer"/>.
    /// The <see cref="WebApplicationFactory{TEntryPoint}"/> will find the entry point class of <typeparamref name="TEntryPoint"/>
    /// assembly and initialize the application by calling <c>IWebHostBuilder CreateWebHostBuilder(string [] args)</c>
    /// on <typeparamref name="TEntryPoint"/>.
    /// </para>
    /// <para>
    /// This constructor will infer the application content root path by searching for a
    /// <see cref="WebApplicationFactoryContentRootAttribute"/> on the assembly containing the functional tests with
    /// a key equal to the <typeparamref name="TEntryPoint"/> assembly <see cref="Assembly.FullName"/>.
    /// In case an attribute with the right key can't be found, <see cref="WebApplicationFactory{TEntryPoint}"/>
    /// will fall back to searching for a solution file (*.sln) and then appending <typeparamref name="TEntryPoint"/> assembly name
    /// to the solution directory. The application root directory will be used to discover views and content files.
    /// </para>
    /// <para>
    /// The application assemblies will be loaded from the dependency context of the assembly containing
    /// <typeparamref name="TEntryPoint" />. This means that project dependencies of the assembly containing
    /// <typeparamref name="TEntryPoint" /> will be loaded as application assemblies.
    /// </para>
    /// </summary>
    public WebApplicationFactory()
    {
        _configuration = ConfigureWebHost;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="WebApplicationFactory{TEntryPoint}"/> class.
    /// </summary>
    ~WebApplicationFactory()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets the <see cref="TestServer"/> created by this <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    public TestServer Server
    {
        get
        {
            if (_useKestrel)
            {
                throw new NotSupportedException(Resources.TestServerNotSupportedWhenUsingKestrel);
            }

            StartServer();
            return _server!;
        }
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> created by the server associated with this <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    public virtual IServiceProvider Services
    {
        get
        {
            StartServer();
            if (_useKestrel)
            {
                return _webHost!.Services;
            }

            return _host?.Services ?? _server!.Host.Services;
        }
    }

    /// <summary>
    /// Helps determine if the `StartServer` method has been called already.
    /// </summary>
    private bool ServerStarted => _webHost != null || _host != null || _server != null;

    /// <summary>
    /// Gets the <see cref="IReadOnlyList{WebApplicationFactory}"/> of factories created from this factory
    /// by further customizing the <see cref="IWebHostBuilder"/> when calling
    /// <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder(Action{IWebHostBuilder})"/>.
    /// </summary>
    public IReadOnlyList<WebApplicationFactory<TEntryPoint>> Factories => _derivedFactories.AsReadOnly();

    /// <summary>
    /// Gets the <see cref="WebApplicationFactoryClientOptions"/> used by <see cref="CreateClient()"/>.
    /// </summary>
    public WebApplicationFactoryClientOptions ClientOptions { get; private set; } = new WebApplicationFactoryClientOptions();

    /// <summary>
    /// Creates a new <see cref="WebApplicationFactory{TEntryPoint}"/> with a <see cref="IWebHostBuilder"/>
    /// that is further customized by <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">
    /// An <see cref="Action{IWebHostBuilder}"/> to configure the <see cref="IWebHostBuilder"/>.
    /// </param>
    /// <returns>A new <see cref="WebApplicationFactory{TEntryPoint}"/>.</returns>
    public WebApplicationFactory<TEntryPoint> WithWebHostBuilder(Action<IWebHostBuilder> configuration) =>
        WithWebHostBuilderCore(configuration);

    internal virtual WebApplicationFactory<TEntryPoint> WithWebHostBuilderCore(Action<IWebHostBuilder> configuration)
    {
        var factory = new DelegatedWebApplicationFactory(
            ClientOptions,
            CreateServer,
            CreateHost,
            CreateWebHostBuilder,
            CreateHostBuilder,
            GetTestAssemblies,
            ConfigureClient,
            builder =>
            {
                _configuration(builder);
                configuration(builder);
            });

        _derivedFactories.Add(factory);

        return factory;
    }

    /// <summary>
    /// Configures the factory to use Kestrel as the server.
    /// </summary>
    public void UseKestrel()
    {
        if (ServerStarted)
        {
            throw new InvalidOperationException(Resources.UseKestrelCanBeCalledBeforeInitialization);
        }

        _useKestrel = true;
    }

    /// <summary>
    /// Configures the factory to use Kestrel as the server.
    /// </summary>
    /// <param name="port">The port to listen to when the server starts. Use `0` to allow dynamic port selection.</param>
    /// <exception cref="InvalidOperationException">Thrown, if this method is called after the WebHostFactory has been initialized.</exception>
    /// <remarks>This method should be called before the factory is initialized either via one of the <see cref="CreateClient()"/> methods
    /// or via the <see cref="StartServer"/> method.</remarks>
    public void UseKestrel(int port)
    {
        UseKestrel();

        this._kestrelPort = port;
    }

    /// <summary>
    /// Configures the factory to use Kestrel as the server.
    /// </summary>
    /// <param name="configureKestrelOptions">A callback handler that will be used for configuring the server when it starts.</param>
    /// <exception cref="InvalidOperationException">Thrown, if this method is called after the WebHostFactory has been initialized.</exception>
    /// <remarks>This method should be called before the factory is initialized either via one of the <see cref="CreateClient()"/> methods
    /// or via the <see cref="StartServer"/> method.</remarks>
    public void UseKestrel(Action<KestrelServerOptions> configureKestrelOptions)
    {
        UseKestrel();

        this._configureKestrelOptions = configureKestrelOptions;
    }

    private IWebHost CreateKestrelServer(IWebHostBuilder builder)
    {
        ConfigureBuilderToUseKestrel(builder);

        var host = builder.Build();

        TryConfigureServerPort(() => GetServerAddressFeature(host));

        host.Start();
        return host;
    }

    private void TryConfigureServerPort(Func<IServerAddressesFeature?> serverAddressFeatureAccessor)
    {
        if (_kestrelPort.HasValue)
        {
            var saf = serverAddressFeatureAccessor();
            if (saf is not null)
            {
                saf.Addresses.Clear();
                saf.Addresses.Add($"http://127.0.0.1:{_kestrelPort}");
                saf.PreferHostingUrls = true;
            }
        }
    }

    private void ConfigureBuilderToUseKestrel(IWebHostBuilder builder)
    {
        if (_configureKestrelOptions is not null)
        {
            builder.UseKestrel(_configureKestrelOptions);
        }
        else
        {
            builder.UseKestrel();
        }
    }

    /// <summary>
    /// Initializes the instance by configurating the host builder.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the provided <typeparamref name="TEntryPoint"/> type has no factory method.</exception>
    public void StartServer()
    {
        if (ServerStarted)
        {
            return;
        }

        EnsureDepsFile();

        var hostBuilder = CreateHostBuilder();
        if (hostBuilder is not null)
        {
            ConfigureHostBuilder(hostBuilder);
            return;
        }

        var builder = CreateWebHostBuilder();
        if (builder is null)
        {
            var deferredHostBuilder = new DeferredHostBuilder();
            deferredHostBuilder.UseEnvironment(Environments.Development);
            // There's no helper for UseApplicationName, but we need to 
            // set the application name to the target entry point 
            // assembly name.
            deferredHostBuilder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                        { HostDefaults.ApplicationKey, typeof(TEntryPoint).Assembly.GetName()?.Name ?? string.Empty }
                });
            });
            // This helper call does the hard work to determine if we can fallback to diagnostic source events to get the host instance
            var factory = HostFactoryResolver.ResolveHostFactory(
                typeof(TEntryPoint).Assembly,
                stopApplication: false,
                configureHostBuilder: deferredHostBuilder.ConfigureHostBuilder,
                entrypointCompleted: deferredHostBuilder.EntryPointCompleted);

            if (factory is not null)
            {
                // If we have a valid factory it means the specified entry point's assembly can potentially resolve the IHost
                // so we set the factory on the DeferredHostBuilder so we can invoke it on the call to IHostBuilder.Build.
                deferredHostBuilder.SetHostFactory(factory);

                ConfigureHostBuilder(deferredHostBuilder);
                return;
            }

            throw new InvalidOperationException(Resources.FormatMissingBuilderMethod(
                nameof(IHostBuilder),
                nameof(IWebHostBuilder),
                typeof(TEntryPoint).Assembly.EntryPoint!.DeclaringType!.FullName,
                typeof(WebApplicationFactory<TEntryPoint>).Name,
                nameof(CreateHostBuilder),
                nameof(CreateWebHostBuilder)));
        }
        else
        {
            SetContentRoot(builder);
            _configuration(builder);
            if (_useKestrel)
            {
                _webHost = CreateKestrelServer(builder);

                TryExtractHostAddress(GetServerAddressFeature(_webHost));
            }
            else
            {
                _server = CreateServer(builder);
            }
        }
    }

    private void TryExtractHostAddress(IServerAddressesFeature? serverAddressFeature)
    {
        if (serverAddressFeature?.Addresses.Count > 0)
        {
            // Store the web host address as it's going to be used every time a client is created to communicate to the server
            _webHostAddress = new Uri(serverAddressFeature.Addresses.Last());
            ClientOptions.BaseAddress = _webHostAddress;
        }
    }

    private void ConfigureHostBuilder(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureWebHost(webHostBuilder =>
        {
            SetContentRoot(webHostBuilder);
            _configuration(webHostBuilder);
            if (_useKestrel)
            {
                ConfigureBuilderToUseKestrel(webHostBuilder);
            }
            else
            {
                webHostBuilder.UseTestServer();
            }
        });
        _host = CreateHost(hostBuilder);
        if (_useKestrel)
        {
            TryExtractHostAddress(GetServerAddressFeature(_host));
        }
        else
        {
            _server = (TestServer)_host.Services.GetRequiredService<IServer>();
        }
    }

    private void SetContentRoot(IWebHostBuilder builder)
    {
        if (SetContentRootFromSetting(builder))
        {
            return;
        }

        var fromFile = File.Exists("MvcTestingAppManifest.json");
        var contentRoot = fromFile ? GetContentRootFromFile("MvcTestingAppManifest.json") : GetContentRootFromAssembly();

        if (contentRoot != null)
        {
            builder.UseContentRoot(contentRoot);
        }
        else
        {
            builder.UseSolutionRelativeContentRoot(typeof(TEntryPoint).Assembly.GetName().Name!);
        }
    }

    private static string? GetContentRootFromFile(string file)
    {
        var data = JsonSerializer.Deserialize(File.ReadAllBytes(file), CustomJsonSerializerContext.Default.IDictionaryStringString)!;
        var key = typeof(TEntryPoint).Assembly.GetName().FullName;

        // If the `ContentRoot` is not provided in the app manifest, then return null
        // and fallback to setting the content root relative to the entrypoint's assembly.
        if (!data.TryGetValue(key, out var contentRoot))
        {
            return null;
        }

        return (contentRoot == "~") ? AppContext.BaseDirectory : contentRoot;
    }

    [JsonSerializable(typeof(IDictionary<string, string>))]
    private sealed partial class CustomJsonSerializerContext : JsonSerializerContext;

    private string? GetContentRootFromAssembly()
    {
        var metadataAttributes = GetContentRootMetadataAttributes(
            typeof(TEntryPoint).Assembly.FullName!,
            typeof(TEntryPoint).Assembly.GetName().Name!);

        string? contentRoot = null;
        for (var i = 0; i < metadataAttributes.Length; i++)
        {
            var contentRootAttribute = metadataAttributes[i];
            var contentRootCandidate = Path.Combine(
                AppContext.BaseDirectory,
                contentRootAttribute.ContentRootPath);

            var contentRootMarker = Path.Combine(
                contentRootCandidate,
                Path.GetFileName(contentRootAttribute.ContentRootTest));

            if (File.Exists(contentRootMarker))
            {
                contentRoot = contentRootCandidate;
                break;
            }
        }

        return contentRoot;
    }

    private static bool SetContentRootFromSetting(IWebHostBuilder builder)
    {
        // Attempt to look for TEST_CONTENTROOT_APPNAME in settings. This should result in looking for
        // ASPNETCORE_TEST_CONTENTROOT_APPNAME environment variable.
        var assemblyName = typeof(TEntryPoint).Assembly.GetName().Name!;
        var settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_");
        var settingName = $"TEST_CONTENTROOT_{settingSuffix}";

        var settingValue = builder.GetSetting(settingName);
        if (settingValue == null)
        {
            return false;
        }

        builder.UseContentRoot(settingValue);
        return true;
    }

    private WebApplicationFactoryContentRootAttribute[] GetContentRootMetadataAttributes(
        string tEntryPointAssemblyFullName,
        string tEntryPointAssemblyName)
    {
        var testAssembly = GetTestAssemblies();
        var metadataAttributes = testAssembly
            .SelectMany(a => a.GetCustomAttributes<WebApplicationFactoryContentRootAttribute>())
            .Where(a => string.Equals(a.Key, tEntryPointAssemblyFullName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.Key, tEntryPointAssemblyName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Priority)
            .ToArray();

        return metadataAttributes;
    }

    /// <summary>
    /// Gets the assemblies containing the functional tests. The
    /// <see cref="WebApplicationFactoryContentRootAttribute"/> applied to these
    /// assemblies defines the content root to use for the given
    /// <typeparamref name="TEntryPoint"/>.
    /// </summary>
    /// <returns>The list of <see cref="Assembly"/> containing tests.</returns>
    protected virtual IEnumerable<Assembly> GetTestAssemblies()
    {
        try
        {
            // The default dependency context will be populated in .net core applications.
            var context = DependencyContext.Default;
            if (context == null || context.CompileLibraries.Count == 0)
            {
                // The app domain friendly name will be populated in full framework.
                return new[] { Assembly.Load(AppDomain.CurrentDomain.FriendlyName) };
            }

            var runtimeProjectLibraries = context.RuntimeLibraries
                .ToDictionary(r => r.Name, r => r, StringComparer.Ordinal);

            // Find the list of projects
            var projects = context.CompileLibraries.Where(l => l.Type == "project");

            var entryPointAssemblyName = typeof(TEntryPoint).Assembly.GetName().Name;

            // Find the list of projects referencing TEntryPoint.
            var candidates = context.CompileLibraries
                .Where(library => library.Dependencies.Any(d => string.Equals(d.Name, entryPointAssemblyName, StringComparison.Ordinal)));

            var testAssemblies = new List<Assembly>();
            foreach (var candidate in candidates)
            {
                if (runtimeProjectLibraries.TryGetValue(candidate.Name, out var runtimeLibrary))
                {
                    var runtimeAssemblies = runtimeLibrary.GetDefaultAssemblyNames(context);
                    testAssemblies.AddRange(runtimeAssemblies.Select(Assembly.Load));
                }
            }

            return testAssemblies;
        }
        catch (Exception)
        {
        }

        return Array.Empty<Assembly>();
    }

    private static void EnsureDepsFile()
    {
        if (typeof(TEntryPoint).Assembly.EntryPoint == null)
        {
            throw new InvalidOperationException(Resources.FormatInvalidAssemblyEntryPoint(typeof(TEntryPoint).Name));
        }

        var depsFileName = $"{typeof(TEntryPoint).Assembly.GetName().Name}.deps.json";
        var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
        if (!depsFile.Exists)
        {
            throw new InvalidOperationException(Resources.FormatMissingDepsFile(
                depsFile.FullName,
                Path.GetFileName(depsFile.FullName)));
        }
    }

    /// <summary>
    /// Creates a <see cref="IHostBuilder"/> used to set up <see cref="TestServer"/>.
    /// </summary>
    /// <remarks>
    /// The default implementation of this method looks for a <c>public static IHostBuilder CreateHostBuilder(string[] args)</c>
    /// method defined on the entry point of the assembly of <typeparamref name="TEntryPoint" /> and invokes it passing an empty string
    /// array as arguments.
    /// </remarks>
    /// <returns>A <see cref="IHostBuilder"/> instance.</returns>
    protected virtual IHostBuilder? CreateHostBuilder()
    {
        var hostBuilder = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(TEntryPoint).Assembly)?.Invoke(Array.Empty<string>());

        hostBuilder?.UseEnvironment(Environments.Development);
        return hostBuilder;
    }

    /// <summary>
    /// Creates a <see cref="IWebHostBuilder"/> used to set up <see cref="TestServer"/>.
    /// </summary>
    /// <remarks>
    /// The default implementation of this method looks for a <c>public static IWebHostBuilder CreateWebHostBuilder(string[] args)</c>
    /// method defined on the entry point of the assembly of <typeparamref name="TEntryPoint" /> and invokes it passing an empty string
    /// array as arguments.
    /// </remarks>
    /// <returns>A <see cref="IWebHostBuilder"/> instance.</returns>
    protected virtual IWebHostBuilder? CreateWebHostBuilder()
    {
        var builder = WebHostBuilderFactory.CreateFromTypesAssemblyEntryPoint<TEntryPoint>(Array.Empty<string>());

        if (builder is not null)
        {
            return builder.UseEnvironment(Environments.Development);
        }

        return null;
    }

    /// <summary>
    /// Creates the <see cref="TestServer"/> with the bootstrapped application in <paramref name="builder"/>.
    /// This is only called for applications using <see cref="IWebHostBuilder"/>. Applications based on
    /// <see cref="IHostBuilder"/> will use <see cref="CreateHost"/> instead.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/> used to
    /// create the server.</param>
    /// <returns>The <see cref="TestServer"/> with the bootstrapped application.</returns>
    protected virtual TestServer CreateServer(IWebHostBuilder builder) => new(builder);

    /// <summary>
    /// Creates the <see cref="IHost"/> with the bootstrapped application in <paramref name="builder"/>.
    /// This is only called for applications using <see cref="IHostBuilder"/>. Applications based on
    /// <see cref="IWebHostBuilder"/> will use <see cref="CreateServer"/> instead.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> used to create the host.</param>
    /// <returns>The <see cref="IHost"/> with the bootstrapped application.</returns>
    protected virtual IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        TryConfigureServerPort(() => GetServerAddressFeature(host));
        host.Start();
        return host;
    }

    private static IServerAddressesFeature? GetServerAddressFeature(IHost host) => host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();

    private static IServerAddressesFeature? GetServerAddressFeature(IWebHost webHost) => webHost.ServerFeatures.Get<IServerAddressesFeature>();

    /// <summary>
    /// Gives a fixture an opportunity to configure the application before it gets built.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/> for the application.</param>
    protected virtual void ConfigureWebHost(IWebHostBuilder builder)
    {
    }

    /// <summary>
    /// Creates an instance of <see cref="HttpClient"/> that automatically follows
    /// redirects and handles cookies.
    /// </summary>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public HttpClient CreateClient()
    {
        var client = CreateClient(ClientOptions);

        if (_useKestrel && object.ReferenceEquals(client.BaseAddress, WebApplicationFactoryClientOptions.DefaultBaseAddres))
        {
            // When using Kestrel, the server may start to listen on a pre-configured port,
            // which can differ from the one configured via ClientOptions.
            // Hence, if the ClientOptions haven't been set explicitly to a custom value, we will assume that
            // the user wants the client to communicate on a port that the server listens too, and because that port may be different, we overwrite it here.
            client.BaseAddress = ClientOptions.BaseAddress;
        }

        return client;
    }

    /// <summary>
    /// Creates an instance of <see cref="HttpClient"/> that automatically follows
    /// redirects and handles cookies.
    /// </summary>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions options) =>
        CreateDefaultClient(options.BaseAddress, options.CreateHandlers());

    /// <summary>
    /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
    /// send <see cref="HttpRequestMessage"/> to the server. The base address of the <see cref="HttpClient"/>
    /// instance will be set to <c>http://localhost</c>.
    /// </summary>
    /// <param name="handlers">A list of <see cref="DelegatingHandler"/> instances to set up on the
    /// <see cref="HttpClient"/>.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public HttpClient CreateDefaultClient(params DelegatingHandler[] handlers)
    {
        StartServer();

        HttpClient client;
        if (handlers == null || handlers.Length == 0)
        {
            if (_useKestrel)
            {
                client = new HttpClient();
            }
            else
            {
                if (_server is null)
                {
                    throw new InvalidOperationException(Resources.ServerNotInitialized);
                }

                client = _server.CreateClient();
            }
        }
        else
        {
            for (var i = handlers.Length - 1; i > 0; i--)
            {
                handlers[i - 1].InnerHandler = handlers[i];
            }

            var serverHandler = CreateHandler();
            handlers[^1].InnerHandler = serverHandler;

            client = new HttpClient(handlers[0]);
        }

        _clients.Add(client);

        ConfigureClient(client);

        return client;
    }

    private HttpMessageHandler CreateHandler()
    {
        if (_useKestrel)
        {
            return new HttpClientHandler();
        }

        if (_server is null)
        {
            throw new InvalidOperationException(Resources.ServerNotInitialized);
        }

        return _server.CreateHandler();
    }

    /// <summary>
    /// Configures <see cref="HttpClient"/> instances created by this <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance getting configured.</param>
    protected virtual void ConfigureClient(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (_useKestrel)
        {
            if (_webHost is null && _host is null)
            {
                throw new InvalidOperationException(Resources.ServerNotInitialized);
            }

            client.BaseAddress = _webHostAddress;
        }
        else
        {
            client.BaseAddress = new Uri("http://localhost");
        }
    }

    /// <summary>
    /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
    /// send <see cref="HttpRequestMessage"/> to the server.
    /// </summary>
    /// <param name="baseAddress">The base address of the <see cref="HttpClient"/> instance.</param>
    /// <param name="handlers">A list of <see cref="DelegatingHandler"/> instances to set up on the
    /// <see cref="HttpClient"/>.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public HttpClient CreateDefaultClient(Uri baseAddress, params DelegatingHandler[] handlers)
    {
        var client = CreateDefaultClient(handlers);
        client.BaseAddress = baseAddress;

        return client;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources;
    /// <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (!_disposedAsync)
            {
                DisposeAsync()
                    .AsTask()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        _disposed = true;
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_disposedAsync)
        {
            return;
        }

        foreach (var client in _clients)
        {
            client.Dispose();
        }

        foreach (var factory in _derivedFactories)
        {
            await ((IAsyncDisposable)factory).DisposeAsync().ConfigureAwait(false);
        }

        _server?.Dispose();

        if (_host != null)
        {
            await _host.StopAsync().ConfigureAwait(false);
            _host?.Dispose();
        }

        _disposedAsync = true;

        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }

    private sealed class DelegatedWebApplicationFactory : WebApplicationFactory<TEntryPoint>
    {
        private readonly Func<IWebHostBuilder, TestServer> _createServer;
        private readonly Func<IHostBuilder, IHost> _createHost;
        private readonly Func<IWebHostBuilder?> _createWebHostBuilder;
        private readonly Func<IHostBuilder?> _createHostBuilder;
        private readonly Func<IEnumerable<Assembly>> _getTestAssemblies;
        private readonly Action<HttpClient> _configureClient;

        public DelegatedWebApplicationFactory(
            WebApplicationFactoryClientOptions options,
            Func<IWebHostBuilder, TestServer> createServer,
            Func<IHostBuilder, IHost> createHost,
            Func<IWebHostBuilder?> createWebHostBuilder,
            Func<IHostBuilder?> createHostBuilder,
            Func<IEnumerable<Assembly>> getTestAssemblies,
            Action<HttpClient> configureClient,
            Action<IWebHostBuilder> configureWebHost)
        {
            ClientOptions = new WebApplicationFactoryClientOptions(options);
            _createServer = createServer;
            _createHost = createHost;
            _createWebHostBuilder = createWebHostBuilder;
            _createHostBuilder = createHostBuilder;
            _getTestAssemblies = getTestAssemblies;
            _configureClient = configureClient;
            _configuration = configureWebHost;
        }

        protected override TestServer CreateServer(IWebHostBuilder builder) => _createServer(builder);

        protected override IHost CreateHost(IHostBuilder builder) => _createHost(builder);

        protected override IWebHostBuilder? CreateWebHostBuilder() => _createWebHostBuilder();

        protected override IHostBuilder? CreateHostBuilder() => _createHostBuilder();

        protected override IEnumerable<Assembly> GetTestAssemblies() => _getTestAssemblies();

        protected override void ConfigureWebHost(IWebHostBuilder builder) => _configuration(builder);

        protected override void ConfigureClient(HttpClient client) => _configureClient(client);

        internal override WebApplicationFactory<TEntryPoint> WithWebHostBuilderCore(Action<IWebHostBuilder> configuration)
        {
            return new DelegatedWebApplicationFactory(
                ClientOptions,
                _createServer,
                _createHost,
                _createWebHostBuilder,
                _createHostBuilder,
                _getTestAssemblies,
                _configureClient,
                builder =>
                {
                    _configuration(builder);
                    configuration(builder);
                });
        }
    }
}
