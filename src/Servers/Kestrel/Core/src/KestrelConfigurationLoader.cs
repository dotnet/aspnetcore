// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel;

/// <summary>
/// Configuration loader for Kestrel.
/// </summary>
public class KestrelConfigurationLoader
{
    private bool _loaded;

    internal KestrelConfigurationLoader(
        KestrelServerOptions options,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        bool reloadOnChange,
        ILogger<KestrelServer> logger,
        ILogger<HttpsConnectionMiddleware> httpsLogger)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        HttpsLogger = httpsLogger ?? throw new ArgumentNullException(nameof(logger));

        ReloadOnChange = reloadOnChange;

        ConfigurationReader = new ConfigurationReader(configuration);
        CertificateConfigLoader = new CertificateConfigLoader(hostEnvironment, logger);
    }

    /// <summary>
    /// Gets the <see cref="KestrelServerOptions"/>.
    /// </summary>
    public KestrelServerOptions Options { get; }

    /// <summary>
    /// Gets the application <see cref="IConfiguration"/>.
    /// </summary>
    public IConfiguration Configuration { get; internal set; }

    /// <summary>
    /// If <see langword="true" />, Kestrel will dynamically update endpoint bindings when configuration changes.
    /// This will only reload endpoints defined in the "Endpoints" section of your Kestrel configuration. Endpoints defined in code will not be reloaded.
    /// </summary>
    internal bool ReloadOnChange { get; }

    private IHostEnvironment HostEnvironment { get; }
    private ILogger<KestrelServer> Logger { get; }
    private ILogger<HttpsConnectionMiddleware> HttpsLogger { get; }

    private ConfigurationReader ConfigurationReader { get; set; }

    private ICertificateConfigLoader CertificateConfigLoader { get; }

    private IDictionary<string, Action<EndpointConfiguration>> EndpointConfigurations { get; }
        = new Dictionary<string, Action<EndpointConfiguration>>(0, StringComparer.OrdinalIgnoreCase);

    // Actions that will be delayed until Load so that they aren't applied if the configuration loader is replaced.
    private IList<Action> EndpointsToAdd { get; } = new List<Action>();

    private CertificateConfig? DefaultCertificateConfig { get; set; }

    /// <summary>
    /// Specifies a configuration Action to run when an endpoint with the given name is loaded from configuration.
    /// </summary>
    public KestrelConfigurationLoader Endpoint(string name, Action<EndpointConfiguration> configureOptions)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        EndpointConfigurations[name] = configureOptions ?? throw new ArgumentNullException(nameof(configureOptions));
        return this;
    }

    /// <summary>
    /// Bind to given IP address and port.
    /// </summary>
    public KestrelConfigurationLoader Endpoint(IPAddress address, int port) => Endpoint(address, port, _ => { });

    /// <summary>
    /// Bind to given IP address and port.
    /// </summary>
    public KestrelConfigurationLoader Endpoint(IPAddress address, int port, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(address);

        return Endpoint(new IPEndPoint(address, port), configure);
    }

    /// <summary>
    /// Bind to given IP endpoint.
    /// </summary>
    public KestrelConfigurationLoader Endpoint(IPEndPoint endPoint) => Endpoint(endPoint, _ => { });

    /// <summary>
    /// Bind to given IP address and port.
    /// </summary>
    public KestrelConfigurationLoader Endpoint(IPEndPoint endPoint, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(configure);

        EndpointsToAdd.Add(() =>
        {
            Options.Listen(endPoint, configure);
        });

        return this;
    }

    /// <summary>
    /// Listens on ::1 and 127.0.0.1 with the given port. Requesting a dynamic port by specifying 0 is not supported
    /// for this type of endpoint.
    /// </summary>
    public KestrelConfigurationLoader LocalhostEndpoint(int port) => LocalhostEndpoint(port, options => { });

    /// <summary>
    /// Listens on ::1 and 127.0.0.1 with the given port. Requesting a dynamic port by specifying 0 is not supported
    /// for this type of endpoint.
    /// </summary>
    public KestrelConfigurationLoader LocalhostEndpoint(int port, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EndpointsToAdd.Add(() =>
        {
            Options.ListenLocalhost(port, configure);
        });

        return this;
    }

    /// <summary>
    /// Listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
    /// </summary>
    public KestrelConfigurationLoader AnyIPEndpoint(int port) => AnyIPEndpoint(port, options => { });

    /// <summary>
    /// Listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
    /// </summary>
    public KestrelConfigurationLoader AnyIPEndpoint(int port, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EndpointsToAdd.Add(() =>
        {
            Options.ListenAnyIP(port, configure);
        });

        return this;
    }

    /// <summary>
    /// Bind to given Unix domain socket path.
    /// </summary>
    public KestrelConfigurationLoader UnixSocketEndpoint(string socketPath) => UnixSocketEndpoint(socketPath, _ => { });

    /// <summary>
    /// Bind to given Unix domain socket path.
    /// </summary>
    public KestrelConfigurationLoader UnixSocketEndpoint(string socketPath, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(socketPath);
        if (socketPath.Length == 0 || socketPath[0] != '/')
        {
            throw new ArgumentException(CoreStrings.UnixSocketPathMustBeAbsolute, nameof(socketPath));
        }
        ArgumentNullException.ThrowIfNull(configure);

        EndpointsToAdd.Add(() =>
        {
            Options.ListenUnixSocket(socketPath, configure);
        });

        return this;
    }

    /// <summary>
    /// Open a socket file descriptor.
    /// </summary>
    public KestrelConfigurationLoader HandleEndpoint(ulong handle) => HandleEndpoint(handle, _ => { });

    /// <summary>
    /// Open a socket file descriptor.
    /// </summary>
    public KestrelConfigurationLoader HandleEndpoint(ulong handle, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EndpointsToAdd.Add(() =>
        {
            Options.ListenHandle(handle, configure);
        });

        return this;
    }

    // Called from KestrelServerOptions.ApplyEndpointDefaults so it applies to even explicit Listen endpoints.
    // Does not require a call to Load.
    internal void ApplyEndpointDefaults(ListenOptions listenOptions)
    {
        var defaults = ConfigurationReader.EndpointDefaults;

        if (defaults.Protocols.HasValue)
        {
            listenOptions.Protocols = defaults.Protocols.Value;
        }
    }

    // Called from KestrelServerOptions.ApplyHttpsDefaults so it applies to even explicit Listen endpoints.
    // Does not require a call to Load.
    internal void ApplyHttpsDefaults(HttpsConnectionAdapterOptions httpsOptions)
    {
        var defaults = ConfigurationReader.EndpointDefaults;

        if (defaults.SslProtocols.HasValue)
        {
            httpsOptions.SslProtocols = defaults.SslProtocols.Value;
        }

        if (defaults.ClientCertificateMode.HasValue)
        {
            httpsOptions.ClientCertificateMode = defaults.ClientCertificateMode.Value;
        }
    }

    /// <summary>
    /// Loads the configuration.
    /// </summary>
    public void Load()
    {
        if (_loaded)
        {
            // The loader has already been run.
            return;
        }
        _loaded = true;

        Reload();

        foreach (var action in EndpointsToAdd)
        {
            action();
        }
    }

    // Adds endpoints from config to KestrelServerOptions.ConfigurationBackedListenOptions and configures some other options.
    // Any endpoints that were removed from the last time endpoints were loaded are returned.
    internal (List<ListenOptions>, List<ListenOptions>) Reload()
    {
        var endpointsToStop = Options.ConfigurationBackedListenOptions.ToList();
        var endpointsToStart = new List<ListenOptions>();

        Options.ConfigurationBackedListenOptions.Clear();
        DefaultCertificateConfig = null;

        ConfigurationReader = new ConfigurationReader(Configuration);

        LoadDefaultCert();

        foreach (var endpoint in ConfigurationReader.Endpoints)
        {
            var listenOptions = AddressBinder.ParseAddress(endpoint.Url, out var https);

            if (!https)
            {
                ConfigurationReader.ThrowIfContainsHttpsOnlyConfiguration(endpoint);
            }

            Options.ApplyEndpointDefaults(listenOptions);

            if (endpoint.Protocols.HasValue)
            {
                listenOptions.Protocols = endpoint.Protocols.Value;
            }
            else
            {
                // Ensure endpoint is reloaded if it used the default protocol and the protocol changed.
                // listenOptions.Protocols should already be set to this by ApplyEndpointDefaults.
                endpoint.Protocols = ConfigurationReader.EndpointDefaults.Protocols;
            }

            // Compare to UseHttps(httpsOptions => { })
            var httpsOptions = new HttpsConnectionAdapterOptions();

            if (https)
            {
                // Defaults
                Options.ApplyHttpsDefaults(httpsOptions);

                if (endpoint.SslProtocols.HasValue)
                {
                    httpsOptions.SslProtocols = endpoint.SslProtocols.Value;
                }
                else
                {
                    // Ensure endpoint is reloaded if it used the default protocol and the SslProtocols changed.
                    endpoint.SslProtocols = ConfigurationReader.EndpointDefaults.SslProtocols;
                }

                if (endpoint.ClientCertificateMode.HasValue)
                {
                    httpsOptions.ClientCertificateMode = endpoint.ClientCertificateMode.Value;
                }
                else
                {
                    // Ensure endpoint is reloaded if it used the default mode and the ClientCertificateMode changed.
                    endpoint.ClientCertificateMode = ConfigurationReader.EndpointDefaults.ClientCertificateMode;
                }

                // A cert specified directly on the endpoint overrides any defaults.
                var (serverCert, fullChain) = CertificateConfigLoader.LoadCertificate(endpoint.Certificate, endpoint.Name);
                httpsOptions.ServerCertificate = serverCert ?? httpsOptions.ServerCertificate;
                httpsOptions.ServerCertificateChain = fullChain ?? httpsOptions.ServerCertificateChain;

                if (httpsOptions.ServerCertificate == null && httpsOptions.ServerCertificateSelector == null)
                {
                    // Fallback
                    Options.ApplyDefaultCert(httpsOptions);

                    // Ensure endpoint is reloaded if it used the default certificate and the certificate changed.
                    endpoint.Certificate = DefaultCertificateConfig;
                }
            }

            // Now that defaults have been loaded, we can compare to the currently bound endpoints to see if the config changed.
            // There's no reason to rerun an EndpointConfigurations callback if nothing changed.
            var matchingBoundEndpoints = endpointsToStop.Where(o => o.EndpointConfig == endpoint).ToList();

            if (matchingBoundEndpoints.Count > 0)
            {
                endpointsToStop.RemoveAll(o => o.EndpointConfig == endpoint);
                Options.ConfigurationBackedListenOptions.AddRange(matchingBoundEndpoints);
                continue;
            }

            if (EndpointConfigurations.TryGetValue(endpoint.Name, out var configureEndpoint))
            {
                var endpointConfig = new EndpointConfiguration(https, listenOptions, httpsOptions, endpoint.ConfigSection);
                configureEndpoint(endpointConfig);
            }

            // EndpointDefaults or configureEndpoint may have added an https adapter.
            if (https && !listenOptions.IsTls)
            {
                if (endpoint.Sni.Count == 0)
                {
                    if (httpsOptions.ServerCertificate == null && httpsOptions.ServerCertificateSelector == null)
                    {
                        throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
                    }

                    listenOptions.UseHttps(httpsOptions);
                }
                else
                {
                    var sniOptionsSelector = new SniOptionsSelector(endpoint.Name, endpoint.Sni, CertificateConfigLoader,
                        httpsOptions, listenOptions.Protocols, HttpsLogger);
                    var tlsCallbackOptions = new TlsHandshakeCallbackOptions()
                    {
                        OnConnection = SniOptionsSelector.OptionsCallback,
                        HandshakeTimeout = httpsOptions.HandshakeTimeout,
                        OnConnectionState = sniOptionsSelector,
                    };

                    listenOptions.UseHttps(tlsCallbackOptions);
                }
            }

            listenOptions.EndpointConfig = endpoint;

            endpointsToStart.Add(listenOptions);
            Options.ConfigurationBackedListenOptions.Add(listenOptions);
        }

        return (endpointsToStop, endpointsToStart);
    }

    private void LoadDefaultCert()
    {
        if (ConfigurationReader.Certificates.TryGetValue("Default", out var defaultCertConfig))
        {
            var (defaultCert, _ /* cert chain */) = CertificateConfigLoader.LoadCertificate(defaultCertConfig, "Default");
            if (defaultCert != null)
            {
                DefaultCertificateConfig = defaultCertConfig;
                Options.DefaultCertificate = defaultCert;
            }
        }
        else
        {
            var (certificate, certificateConfig) = FindDeveloperCertificateFile();
            if (certificate != null)
            {
                Logger.LocatedDevelopmentCertificate(certificate);
                DefaultCertificateConfig = certificateConfig;
                Options.DefaultCertificate = certificate;
            }
        }
    }

    private (X509Certificate2?, CertificateConfig?) FindDeveloperCertificateFile()
    {
        string? certificatePath = null;
        if (ConfigurationReader.Certificates.TryGetValue("Development", out var certificateConfig) &&
            certificateConfig.Path == null &&
            certificateConfig.Password != null &&
            TryGetCertificatePath(out certificatePath) &&
            File.Exists(certificatePath))
        {
            try
            {
                var certificate = new X509Certificate2(certificatePath, certificateConfig.Password);

                if (IsDevelopmentCertificate(certificate))
                {
                    return (certificate, certificateConfig);
                }
            }
            catch (CryptographicException)
            {
                Logger.FailedToLoadDevelopmentCertificate(certificatePath);
            }
        }
        else if (!string.IsNullOrEmpty(certificatePath))
        {
            Logger.FailedToLocateDevelopmentCertificateFile(certificatePath);
        }

        return (null, null);
    }

    private static bool IsDevelopmentCertificate(X509Certificate2 certificate)
    {
        if (!string.Equals(certificate.Subject, "CN=localhost", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var ext in certificate.Extensions)
        {
            if (string.Equals(ext.Oid?.Value, CertificateManager.AspNetHttpsOid, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetCertificatePath([NotNullWhen(true)] out string? path)
    {
        // See https://github.com/aspnet/Hosting/issues/1294
        var appData = Environment.GetEnvironmentVariable("APPDATA");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var basePath = appData != null ? Path.Combine(appData, "ASP.NET", "https") : null;
        basePath = basePath ?? (home != null ? Path.Combine(home, ".aspnet", "https") : null);
        path = basePath != null ? Path.Combine(basePath, $"{HostEnvironment.ApplicationName}.pfx") : null;
        return path != null;
    }
}
