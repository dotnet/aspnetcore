// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// A factory for DirectTls (native, fd-bound OpenSSL) connections. Binds only <see cref="DirectTlsEndpoint"/>
/// endpoints; every other endpoint type is left to the default transport.
/// </summary>
internal sealed class DirectTlsTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private readonly DirectTlsTransportOptions _options;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectTlsTransportFactory"/> class.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="applicationLifetime">
    /// The host application lifetime, used to stop the host if a pump fails unrecoverably. Supplied by the DI
    /// container.
    /// </param>
    public DirectTlsTransportFactory(
        IOptions<DirectTlsTransportOptions> options,
        ILoggerFactory loggerFactory,
        IHostApplicationLifetime applicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(applicationLifetime);

        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DirectTlsTransportFactory>();
        _applicationLifetime = applicationLifetime;
    }

    /// <inheritdoc />
    public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("The DirectTls transport requires a Linux operating system.");
        }

        if (endpoint is not DirectTlsEndpoint directTlsEndpoint)
        {
            throw new NotSupportedException(
                $"The DirectTls transport only binds {nameof(DirectTlsEndpoint)} endpoints.");
        }

        var endpointOptions = directTlsEndpoint.Options;

        // DirectTls is TLS-only: a server certificate is mandatory.
        if (endpointOptions.ServerCertificate is null && endpointOptions.ServerCertificateSelector is null)
        {
            throw new InvalidOperationException(
                $"A server certificate is required for a {nameof(DirectTlsEndpoint)}. Set " +
                $"{nameof(DirectTlsEndpointOptions.ServerCertificate)} or " +
                $"{nameof(DirectTlsEndpointOptions.ServerCertificateSelector)} on the endpoint options.");
        }

        if (endpointOptions.ClientCertificateMode == ClientCertificateMode.DelayCertificate)
        {
            throw new NotSupportedException(
                $"{nameof(ClientCertificateMode)}.{nameof(ClientCertificateMode.DelayCertificate)} is not supported by the DirectTls transport.");
        }

        // ALPN list advertised during the handshake, derived from the endpoint's HTTP protocols (server
        // preference h2 first). This is what lets HttpConnection.SelectProtocol negotiate HTTP/2. The
        // protocols were copied from the endpoint's ListenOptions.Protocols by DirectTlsEndpointProtocolsSetup.
        var applicationProtocols = BuildApplicationProtocols(endpointOptions.HttpProtocols);

        bool requireClientCertificate =
            endpointOptions.ClientCertificateMode is ClientCertificateMode.AllowCertificate or ClientCertificateMode.RequireCertificate;

        // Non-null only when the endpoint requests a client certificate. The pump invokes this at handshake
        // completion (the fd fast path cannot surface the mTLS verdict itself) and drops rejected connections.
        RemoteCertificateValidationCallback? clientCertificateValidation =
            requireClientCertificate ? BuildClientCertificateValidation(endpointOptions) : null;

        // Bootstrap context created WITHOUT server credentials. On the runtime's socket-bound (fd) handshake
        // this forces the deferred model: the first Handshake() parses the ClientHello and returns
        // NeedsTlsContext, at which point the pump asks the resolver below for the real per-host context.
        TlsContext bootstrapContext = TlsContext.CreateServer(new SslServerAuthenticationOptions());

        // Per-certificate TlsContext cache so repeated SNI resolutions of the same certificate reuse one
        // native context (creating a TlsContext acquires OpenSSL credentials).
        var contextCache = new ConcurrentDictionary<X509Certificate2, TlsContext>();

        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)> contextResolver =
            (connection, hostName) =>
            {
                var certificate = endpointOptions.ServerCertificateSelector?.Invoke(connection, hostName)
                    ?? endpointOptions.ServerCertificate;

                if (certificate is null)
                {
                    throw new AuthenticationException(
                        $"No server certificate was resolved for SNI host name '{hostName}'.");
                }

                if (!contextCache.TryGetValue(certificate, out var context))
                {
                    var authenticationOptions = new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                        EnabledSslProtocols = endpointOptions.SslProtocols,
                        ApplicationProtocols = applicationProtocols,
                        ClientCertificateRequired = requireClientCertificate,
                    };

                    if (clientCertificateValidation is not null)
                    {
                        authenticationOptions.RemoteCertificateValidationCallback = clientCertificateValidation;
                    }

                    var candidate = TlsContext.CreateServer(authenticationOptions);
                    context = contextCache.GetOrAdd(certificate, candidate);

                    // Multiple threads racing to add the same certificate can create multiple candidates,
                    // so make sure we dont leak TlsContext (with cert/key handles) by disposing the non-cached candidate.
                    if (!ReferenceEquals(context, candidate))
                    {
                        candidate.Dispose();
                    }
                }

                return (context, clientCertificateValidation);
            };

        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = endpointOptions.TlsClientHelloBytesCallback;

        // Each listener owns its own pump pool bound to its own listen socket. This keeps endpoints fully
        // isolated so per-endpoint certificate selection (e.g. two ports with different certs) works
        // correctly, at the cost of WorkerCount threads per endpoint. The endpoint may override the
        // transport-wide worker count so multi-endpoint servers can bound their total thread count.
        var workerCount = endpointOptions.WorkerCount ?? _options.WorkerCount;
        var pumpPool = new TlsEventPumpPool(workerCount, _loggerFactory, endpointOptions.HandshakeTimeout);

        var memoryPool = _options.MemoryPoolFactory.Create(DirectTlsTransportOptions.MemoryPoolOptions);

        // The listener owns the native contexts (bootstrap + per-SNI cache) and disposes them on teardown.
        var ownedServerContexts = new ServerTlsContexts(bootstrapContext, contextCache);

        var transport = new DirectTlsConnectionListener(
            _loggerFactory,
            bootstrapContext,
            contextResolver,
            pumpPool,
            endpoint,
            _options,
            memoryPool,
            _applicationLifetime,
            clientHelloCallback,
            ownedServerContexts,
            serverCertificateSelectorConfigured: endpointOptions.ServerCertificateSelector is not null);

        _logger.LogInformation("DirectTls listener bound for endpoint {Endpoint}.", endpoint);

        try
        {
            transport.Bind();
        }
        catch
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return transport;
    }

    /// <inheritdoc />
    public bool CanBind(EndPoint endpoint)
    {
        // DirectTls binds ONLY endpoints explicitly opted in via DirectTlsEndpoint. Every other endpoint
        // (including plain IPEndPoint) falls through to the default transport.
        return endpoint is DirectTlsEndpoint;
    }

    // Builds the ALPN protocol list from the endpoint's HttpProtocols. Server preference is h2 first, then
    // http/1.1, matching the order the SslStream-based HTTPS path offers.
    private static List<SslApplicationProtocol> BuildApplicationProtocols(HttpProtocols protocols)
    {
        var applicationProtocols = new List<SslApplicationProtocol>();

        if ((protocols & HttpProtocols.Http2) == HttpProtocols.Http2)
        {
            applicationProtocols.Add(SslApplicationProtocol.Http2);
        }

        if ((protocols & HttpProtocols.Http1) == HttpProtocols.Http1)
        {
            applicationProtocols.Add(SslApplicationProtocol.Http11);
        }

        return applicationProtocols;
    }

    // Maps the endpoint's ClientCertificateMode + optional user validation callback onto the
    // RemoteCertificateValidationCallback the pump runs at handshake completion. Only built for
    // Allow/Require modes.
    internal static RemoteCertificateValidationCallback BuildClientCertificateValidation(DirectTlsEndpointOptions endpointOptions)
    {
        var clientCertificateMode = endpointOptions.ClientCertificateMode;
        var userValidation = endpointOptions.ClientCertificateValidation;

        bool ValidateCertificate(X509Certificate2 certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return userValidation is not null
                ? userValidation(certificate, chain, sslPolicyErrors)
                : sslPolicyErrors == SslPolicyErrors.None;
        }

        return (sender, certificate, chain, sslPolicyErrors) =>
        {
            if (certificate is null)
            {
                // AllowCertificate tolerates a missing client cert; RequireCertificate rejects it.
                return clientCertificateMode != ClientCertificateMode.RequireCertificate;
            }

            if (certificate is X509Certificate2 certificate2)
            {
                return ValidateCertificate(certificate2, chain, sslPolicyErrors);
            }

            using var convertedCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));

            return ValidateCertificate(convertedCertificate, chain, sslPolicyErrors);
        };
    }
}
