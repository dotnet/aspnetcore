// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// Provides programmatic configuration of Kestrel-specific features.
/// </summary>
public class KestrelServerOptions
{
    internal const string DisableHttp1LineFeedTerminatorsSwitchKey = "Microsoft.AspNetCore.Server.Kestrel.DisableHttp1LineFeedTerminators";

    // internal to fast-path header decoding when RequestHeaderEncodingSelector is unchanged.
    internal static readonly Func<string, Encoding?> DefaultHeaderEncodingSelector = _ => null;

    private Func<string, Encoding?> _requestHeaderEncodingSelector = DefaultHeaderEncodingSelector;

    private Func<string, Encoding?> _responseHeaderEncodingSelector = DefaultHeaderEncodingSelector;

    // The following two lists configure the endpoints that Kestrel should listen to. If both lists are empty, the "urls" config setting (e.g. UseUrls) is used.
    internal List<ListenOptions> CodeBackedListenOptions { get; } = new List<ListenOptions>();
    internal List<ListenOptions> ConfigurationBackedListenOptions { get; } = new List<ListenOptions>();
    internal IEnumerable<ListenOptions> ListenOptions => CodeBackedListenOptions.Concat(ConfigurationBackedListenOptions);

    // For testing and debugging.
    internal List<ListenOptions> OptionsInUse { get; } = new List<ListenOptions>();

    /// <summary>
    /// Gets or sets whether the <c>Server</c> header should be included in each response.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool AddServerHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value that controls whether dynamic compression of response headers is allowed.
    /// For more information about the security considerations of HPack dynamic header compression, visit
    /// <see href="https://tools.ietf.org/html/rfc7541#section-7"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool AllowResponseHeaderCompression { get; set; } = true;

    /// <summary>
    /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>
    /// </summary>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    public bool AllowSynchronousIO { get; set; }

    /// <summary>
    /// Gets or sets a value that controls how the `:scheme` field for HTTP/2 and HTTP/3 requests is validated.
    /// <para>
    /// If <c>false</c> then the `:scheme` field for HTTP/2 and HTTP/3 requests must exactly match the transport (e.g. https for TLS
    /// connections, http for non-TLS). If <c>true</c> then the `:scheme` field for HTTP/2 and HTTP/3 requests can be set to alternate values
    /// and this will be reflected by `HttpRequest.Scheme`. The Scheme must still be valid according to
    /// <see href="https://datatracker.ietf.org/doc/html/rfc3986/#section-3.1"/>. Only enable this when working with a trusted proxy. This can be used in
    /// scenarios such as proxies converting from alternate protocols. See <see href="https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.3"/>.
    /// Applications that enable this should validate an expected scheme is provided before using it.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool AllowAlternateSchemes { get; set; }

    /// <summary>
    /// Gets or sets a value that controls whether the string values materialized
    /// will be reused across requests; if they match, or if the strings will always be reallocated.
    /// </summary>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    public bool DisableStringReuse { get; set; }

    /// <summary>
    /// Controls whether to return the "Alt-Svc" header from an HTTP/2 or lower response for HTTP/3.
    /// </summary>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    [Obsolete($"This property is obsolete and will be removed in a future version. It no longer has any impact on runtime behavior. Use {nameof(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions)}.{nameof(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions.DisableAltSvcHeader)} to configure \"Alt-Svc\" behavior.", error: true)]
    public bool EnableAltSvc { get; set; }

    /// <summary>
    /// Gets or sets a callback that returns the <see cref="Encoding"/> to decode the value for the specified request header name,
    /// or <see langword="null"/> to use the default <see cref="UTF8Encoding"/>.
    /// </summary>
    public Func<string, Encoding?> RequestHeaderEncodingSelector
    {
        get => _requestHeaderEncodingSelector;
        set => _requestHeaderEncodingSelector = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets a callback that returns the <see cref="Encoding"/> to encode the value for the specified response header
    /// or trailer name, or <see langword="null"/> to use the default <see cref="ASCIIEncoding"/>.
    /// </summary>
    public Func<string, Encoding?> ResponseHeaderEncodingSelector
    {
        get => _responseHeaderEncodingSelector;
        set => _responseHeaderEncodingSelector = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Enables the Listen options callback to resolve and use services registered by the application during startup.
    /// Typically initialized by UseKestrel().
    /// </summary>
    public IServiceProvider ApplicationServices { get; set; } = default!; // This should typically be set

    /// <summary>
    /// Provides access to request limit options.
    /// </summary>
    public KestrelServerLimits Limits { get; } = new KestrelServerLimits();

    /// <summary>
    /// Provides a configuration source where endpoints will be loaded from on server start.
    /// The default is <see langword="null"/>.
    /// </summary>
    public KestrelConfigurationLoader? ConfigurationLoader { get; set; }

    /// <summary>
    /// A default configuration action for all endpoints. Use for Listen, configuration, the default url, and URLs.
    /// </summary>
    private Action<ListenOptions> EndpointDefaults { get; set; } = _ => { };

    /// <summary>
    /// A default configuration action for all https endpoints.
    /// </summary>
    private Action<HttpsConnectionAdapterOptions> HttpsDefaults { get; set; } = _ => { };

    /// <summary>
    /// The default server certificate for https endpoints. This is applied lazily after HttpsDefaults and user options.
    /// </summary>
    internal X509Certificate2? DefaultCertificate { get; set; }

    /// <summary>
    /// Has the default dev certificate load been attempted?
    /// </summary>
    internal bool IsDevCertLoaded { get; set; }

    /// <summary>
    /// Internal AppContext switch to toggle the WebTransport and HTTP/3 datagrams experiemental features.
    /// </summary>
    private bool? _enableWebTransportAndH3Datagrams;
    internal bool EnableWebTransportAndH3Datagrams
    {
        get
        {
            if (!_enableWebTransportAndH3Datagrams.HasValue)
            {
                _enableWebTransportAndH3Datagrams = AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out var enabled) && enabled;
            }

            return _enableWebTransportAndH3Datagrams.Value;
        }
        set => _enableWebTransportAndH3Datagrams = value;
    }

    /// <summary>
    /// Internal AppContext switch to toggle whether a request line can end with LF only instead of CR/LF.
    /// </summary>
    private bool? _disableHttp1LineFeedTerminators;
    internal bool DisableHttp1LineFeedTerminators
    {
        get
        {
            if (!_disableHttp1LineFeedTerminators.HasValue)
            {
                _disableHttp1LineFeedTerminators = AppContext.TryGetSwitch(DisableHttp1LineFeedTerminatorsSwitchKey, out var disabled) && disabled;
            }

            return _disableHttp1LineFeedTerminators.Value;
        }
        set => _disableHttp1LineFeedTerminators = value;
    }

    /// <summary>
    /// Specifies a configuration Action to run for each newly created endpoint. Calling this again will replace
    /// the prior action.
    /// </summary>
    public void ConfigureEndpointDefaults(Action<ListenOptions> configureOptions)
    {
        EndpointDefaults = configureOptions ?? throw new ArgumentNullException(nameof(configureOptions));
    }

    internal void ApplyEndpointDefaults(ListenOptions listenOptions)
    {
        listenOptions.KestrelServerOptions = this;
        ConfigurationLoader?.ApplyEndpointDefaults(listenOptions);
        EndpointDefaults(listenOptions);
    }

    /// <summary>
    /// Specifies a configuration Action to run for each newly created https endpoint. Calling this again will replace
    /// the prior action.
    /// </summary>
    public void ConfigureHttpsDefaults(Action<HttpsConnectionAdapterOptions> configureOptions)
    {
        HttpsDefaults = configureOptions ?? throw new ArgumentNullException(nameof(configureOptions));
    }

    internal void ApplyHttpsDefaults(HttpsConnectionAdapterOptions httpsOptions)
    {
        ConfigurationLoader?.ApplyHttpsDefaults(httpsOptions);
        HttpsDefaults(httpsOptions);
    }

    internal void ApplyDefaultCert(HttpsConnectionAdapterOptions httpsOptions)
    {
        if (httpsOptions.ServerCertificate != null || httpsOptions.ServerCertificateSelector != null)
        {
            return;
        }

        EnsureDefaultCert();

        httpsOptions.ServerCertificate = DefaultCertificate;
    }

    internal void Serialize(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(nameof(AllowSynchronousIO));
        writer.WriteBooleanValue(AllowSynchronousIO);

        writer.WritePropertyName(nameof(AddServerHeader));
        writer.WriteBooleanValue(AddServerHeader);

        writer.WritePropertyName(nameof(AllowAlternateSchemes));
        writer.WriteBooleanValue(AllowAlternateSchemes);

        writer.WritePropertyName(nameof(AllowResponseHeaderCompression));
        writer.WriteBooleanValue(AllowResponseHeaderCompression);

        writer.WritePropertyName(nameof(IsDevCertLoaded));
        writer.WriteBooleanValue(IsDevCertLoaded);

        writer.WriteString(nameof(RequestHeaderEncodingSelector), RequestHeaderEncodingSelector == DefaultHeaderEncodingSelector ? "default" : "configured");
        writer.WriteString(nameof(ResponseHeaderEncodingSelector), ResponseHeaderEncodingSelector == DefaultHeaderEncodingSelector ? "default" : "configured");

        // Limits
        writer.WritePropertyName(nameof(Limits));
        writer.WriteStartObject();
        Limits.Serialize(writer);
        writer.WriteEndObject();

        // ListenOptions
        writer.WritePropertyName(nameof(ListenOptions));
        writer.WriteStartArray();
        foreach (var listenOptions in OptionsInUse)
        {
            writer.WriteStartObject();
            writer.WriteString("Address", listenOptions.GetDisplayName());
            writer.WritePropertyName(nameof(listenOptions.IsTls));
            writer.WriteBooleanValue(listenOptions.IsTls);
            writer.WriteString(nameof(listenOptions.Protocols), listenOptions.Protocols.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private void EnsureDefaultCert()
    {
        if (DefaultCertificate == null && !IsDevCertLoaded)
        {
            IsDevCertLoaded = true; // Only try once
            var logger = ApplicationServices!.GetRequiredService<ILogger<KestrelServer>>();
            try
            {
                DefaultCertificate = CertificateManager.Instance.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true, requireExportable: false)
                    .FirstOrDefault();

                if (DefaultCertificate != null)
                {
                    var status = CertificateManager.Instance.CheckCertificateState(DefaultCertificate, interactive: false);
                    if (!status.Success)
                    {
                        // Display a warning indicating to the user that a prompt might appear and provide instructions on what to do in that
                        // case. The underlying implementation of this check is specific to Mac OS and is handled within CheckCertificateState.
                        // Kestrel must NEVER cause a UI prompt on a production system. We only attempt this here because Mac OS is not supported
                        // in production.
                        Debug.Assert(status.FailureMessage != null, "Status with a failure result must have a message.");
                        logger.DeveloperCertificateFirstRun(status.FailureMessage);

                        // Prevent binding to HTTPS if the certificate is not valid (avoid the prompt)
                        DefaultCertificate = null;
                    }
                    else if (!CertificateManager.Instance.IsTrusted(DefaultCertificate))
                    {
                        logger.DeveloperCertificateNotTrusted();
                    }
                }
                else
                {
                    logger.UnableToLocateDevelopmentCertificate();
                }
            }
            catch
            {
                logger.UnableToLocateDevelopmentCertificate();
            }
        }
    }

    /// <summary>
    /// Creates a configuration loader for setting up Kestrel.
    /// </summary>
    /// <returns>A <see cref="KestrelConfigurationLoader"/> for configuring endpoints.</returns>
    public KestrelConfigurationLoader Configure() => Configure(new ConfigurationBuilder().Build());

    /// <summary>
    /// Creates a configuration loader for setting up Kestrel that takes an <see cref="IConfiguration"/> as input.
    /// This configuration must be scoped to the configuration section for Kestrel.
    /// Call <see cref="Configure(IConfiguration, bool)"/> to enable dynamic endpoint binding updates.
    /// </summary>
    /// <param name="config">The configuration section for Kestrel.</param>
    /// <returns>A <see cref="KestrelConfigurationLoader"/> for further endpoint configuration.</returns>
    public KestrelConfigurationLoader Configure(IConfiguration config) => Configure(config, reloadOnChange: false);

    /// <summary>
    /// Creates a configuration loader for setting up Kestrel that takes an <see cref="IConfiguration"/> as input.
    /// This configuration must be scoped to the configuration section for Kestrel.
    /// </summary>
    /// <param name="config">The configuration section for Kestrel.</param>
    /// <param name="reloadOnChange">
    /// If <see langword="true"/>, Kestrel will dynamically update endpoint bindings when configuration changes.
    /// This will only reload endpoints defined in the "Endpoints" section of your <paramref name="config"/>. Endpoints defined in code will not be reloaded.
    /// </param>
    /// <returns>A <see cref="KestrelConfigurationLoader"/> for further endpoint configuration.</returns>
    public KestrelConfigurationLoader Configure(IConfiguration config, bool reloadOnChange)
    {
        if (ApplicationServices is null)
        {
            throw new InvalidOperationException($"{nameof(ApplicationServices)} must not be null. This is normally set automatically via {nameof(IConfigureOptions<KestrelServerOptions>)}.");
        }

        var hostEnvironment = ApplicationServices.GetRequiredService<IHostEnvironment>();
        var logger = ApplicationServices.GetRequiredService<ILogger<KestrelServer>>();
        var httpsLogger = ApplicationServices.GetRequiredService<ILogger<HttpsConnectionMiddleware>>();

        var loader = new KestrelConfigurationLoader(this, config, hostEnvironment, reloadOnChange, logger, httpsLogger);
        ConfigurationLoader = loader;
        return loader;
    }

    /// <summary>
    /// Bind to the given IP address and port.
    /// </summary>
    public void Listen(IPAddress address, int port)
    {
        Listen(address, port, _ => { });
    }

    /// <summary>
    /// Bind to the given IP address and port.
    /// The callback configures endpoint-specific settings.
    /// </summary>
    public void Listen(IPAddress address, int port, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(address);

        Listen(new IPEndPoint(address, port), configure);
    }

    /// <summary>
    /// Bind to the given IP endpoint.
    /// </summary>
    public void Listen(IPEndPoint endPoint)
    {
        Listen((EndPoint)endPoint);
    }

    /// <summary>
    /// Bind to the given endpoint.
    /// </summary>
    /// <param name="endPoint"></param>
    public void Listen(EndPoint endPoint)
    {
        Listen(endPoint, _ => { });
    }

    /// <summary>
    /// Bind to the given IP address and port.
    /// The callback configures endpoint-specific settings.
    /// </summary>
    public void Listen(IPEndPoint endPoint, Action<ListenOptions> configure)
    {
        Listen((EndPoint)endPoint, configure);
    }

    /// <summary>
    /// Bind to the given endpoint.
    /// The callback configures endpoint-specific settings.
    /// </summary>
    public void Listen(EndPoint endPoint, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(configure);

        var listenOptions = new ListenOptions(endPoint);
        ApplyEndpointDefaults(listenOptions);
        configure(listenOptions);
        CodeBackedListenOptions.Add(listenOptions);
    }

    /// <summary>
    /// Listens on ::1 and 127.0.0.1 with the given port. Requesting a dynamic port by specifying 0 is not supported
    /// for this type of endpoint.
    /// </summary>
    public void ListenLocalhost(int port) => ListenLocalhost(port, options => { });

    /// <summary>
    /// Listens on ::1 and 127.0.0.1 with the given port. Requesting a dynamic port by specifying 0 is not supported
    /// for this type of endpoint.
    /// </summary>
    public void ListenLocalhost(int port, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var listenOptions = new LocalhostListenOptions(port);
        ApplyEndpointDefaults(listenOptions);
        configure(listenOptions);
        CodeBackedListenOptions.Add(listenOptions);
    }

    /// <summary>
    /// Listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
    /// </summary>
    public void ListenAnyIP(int port) => ListenAnyIP(port, options => { });

    /// <summary>
    /// Listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
    /// </summary>
    public void ListenAnyIP(int port, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var listenOptions = new AnyIPListenOptions(port);
        ApplyEndpointDefaults(listenOptions);
        configure(listenOptions);
        CodeBackedListenOptions.Add(listenOptions);
    }

    /// <summary>
    /// Bind to the given Unix domain socket path.
    /// </summary>
    public void ListenUnixSocket(string socketPath)
    {
        ListenUnixSocket(socketPath, _ => { });
    }

    /// <summary>
    /// Bind to the given Unix domain socket path.
    /// Specify callback to configure endpoint-specific settings.
    /// </summary>
    public void ListenUnixSocket(string socketPath, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(socketPath);

        if (!Path.IsPathRooted(socketPath))
        {
            throw new ArgumentException(CoreStrings.UnixSocketPathMustBeAbsolute, nameof(socketPath));
        }
        ArgumentNullException.ThrowIfNull(configure);

        var listenOptions = new ListenOptions(socketPath);
        ApplyEndpointDefaults(listenOptions);
        configure(listenOptions);
        CodeBackedListenOptions.Add(listenOptions);
    }

    /// <summary>
    /// Open a socket file descriptor.
    /// </summary>
    public void ListenHandle(ulong handle)
    {
        ListenHandle(handle, _ => { });
    }

    /// <summary>
    /// Open a socket file descriptor.
    /// The callback configures endpoint-specific settings.
    /// </summary>
    public void ListenHandle(ulong handle, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var listenOptions = new ListenOptions(handle);
        ApplyEndpointDefaults(listenOptions);
        configure(listenOptions);
        CodeBackedListenOptions.Add(listenOptions);
    }

    /// <summary>
    /// Bind to the given named pipe.
    /// </summary>
    public void ListenNamedPipe(string pipeName)
    {
        ListenNamedPipe(pipeName, _ => { });
    }

    /// <summary>
    /// Bind to the given named pipe.
    /// Specify callback to configure endpoint-specific settings.
    /// </summary>
    public void ListenNamedPipe(string pipeName, Action<ListenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(pipeName);
        ArgumentNullException.ThrowIfNull(configure);

        var listenOptions = new ListenOptions(new NamedPipeEndPoint(pipeName));
        ApplyEndpointDefaults(listenOptions);
        configure(listenOptions);
        CodeBackedListenOptions.Add(listenOptions);
    }
}
