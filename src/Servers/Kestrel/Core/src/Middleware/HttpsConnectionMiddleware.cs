// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Security;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

internal sealed class HttpsConnectionMiddleware
{
    private const string EnableWindows81Http2 = "Microsoft.AspNetCore.Server.Kestrel.EnableWindows81Http2";

    private static readonly bool _isWindowsVersionIncompatibleWithHttp2 = IsWindowsVersionIncompatibleWithHttp2();

    private readonly ConnectionDelegate _next;
    private readonly TimeSpan _handshakeTimeout;
    private readonly ILogger<HttpsConnectionMiddleware> _logger;
    private readonly Func<Stream, SslStream> _sslStreamFactory;

    // The following fields are only set by HttpsConnectionAdapterOptions ctor.
    private readonly HttpsConnectionAdapterOptions? _options;
    private readonly KestrelMetrics _metrics;
    private readonly SslStreamCertificateContext? _serverCertificateContext;
    private readonly X509Certificate2? _serverCertificate;
    private readonly Func<ConnectionContext, string?, X509Certificate2?>? _serverCertificateSelector;

    // The following fields are only set by TlsHandshakeCallbackOptions ctor.
    private readonly Func<TlsHandshakeCallbackContext, ValueTask<SslServerAuthenticationOptions>>? _tlsCallbackOptions;
    private readonly object? _tlsCallbackOptionsState;

    // Internal for testing
    internal readonly HttpProtocols _httpProtocols;

    // Pool for cancellation tokens that cancel the handshake
    private readonly CancellationTokenSourcePool _ctsPool = new();

    public HttpsConnectionMiddleware(ConnectionDelegate next, HttpsConnectionAdapterOptions options, HttpProtocols httpProtocols, KestrelMetrics metrics)
      : this(next, options, httpProtocols, loggerFactory: NullLoggerFactory.Instance, metrics: metrics)
    {
    }

    public HttpsConnectionMiddleware(ConnectionDelegate next, HttpsConnectionAdapterOptions options, HttpProtocols httpProtocols, ILoggerFactory loggerFactory, KestrelMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.HasServerCertificateOrSelector)
        {
            throw new ArgumentException(CoreStrings.ServerCertificateRequired, nameof(options));
        }

        _next = next;
        _handshakeTimeout = options.HandshakeTimeout;
        _logger = loggerFactory.CreateLogger<HttpsConnectionMiddleware>();
        _metrics = metrics;

        // Something similar to the following could allow us to remove more duplicate logic, but we need https://github.com/dotnet/runtime/issues/40402 to be fixed first.
        //var sniOptionsSelector = new SniOptionsSelector("", new Dictionary<string, SniConfig> { { "*", new SniConfig() } }, new NoopCertificateConfigLoader(), options, options.HttpProtocols, _logger);
        //_httpsOptionsCallback = SniOptionsSelector.OptionsCallback;
        //_httpsOptionsCallbackState = sniOptionsSelector;
        //_sslStreamFactory = s => new SslStream(s);

        _options = options;
        _httpProtocols = ValidateAndNormalizeHttpProtocols(httpProtocols, _logger);

        // capture the certificate now so it can't be switched after validation
        _serverCertificate = options.ServerCertificate;
        _serverCertificateSelector = options.ServerCertificateSelector;

        // If a selector is provided then ignore the cert, it may be a default cert.
        if (_serverCertificateSelector != null)
        {
            // SslStream doesn't allow both.
            _serverCertificate = null;
        }
        else
        {
            Debug.Assert(_serverCertificate != null);

            EnsureCertificateIsAllowedForServerAuth(_serverCertificate, _logger);

            var certificate = _serverCertificate;
            if (!certificate.HasPrivateKey)
            {
                // SslStream historically has logic to deal with certificate missing private keys.
                // By resolving the SslStreamCertificateContext eagerly, we circumvent this logic so
                // try to resolve the certificate from the store if there's no private key in the cert.
                certificate = LocateCertificateWithPrivateKey(certificate);
            }

            // This might be do blocking IO but it'll resolve the certificate chain up front before any connections are
            // made to the server
            _serverCertificateContext = SslStreamCertificateContext.Create(certificate, additionalCertificates: options.ServerCertificateChain);
        }

        var remoteCertificateValidationCallback = _options.ClientCertificateMode == ClientCertificateMode.NoCertificate ?
            (RemoteCertificateValidationCallback?)null : RemoteCertificateValidationCallback;

        _sslStreamFactory = s => new SslStream(s, leaveInnerStreamOpen: false, userCertificateValidationCallback: remoteCertificateValidationCallback);
    }

    internal HttpsConnectionMiddleware(
        ConnectionDelegate next,
        TlsHandshakeCallbackOptions tlsCallbackOptions,
        ILoggerFactory loggerFactory,
        KestrelMetrics metrics)
    {
        _next = next;
        _handshakeTimeout = tlsCallbackOptions.HandshakeTimeout;
        _logger = loggerFactory.CreateLogger<HttpsConnectionMiddleware>();
        _metrics = metrics;

        _tlsCallbackOptions = tlsCallbackOptions.OnConnection;
        _tlsCallbackOptionsState = tlsCallbackOptions.OnConnectionState;
        _httpProtocols = ValidateAndNormalizeHttpProtocols(tlsCallbackOptions.HttpProtocols, _logger);
        _sslStreamFactory = s => new SslStream(s);
    }

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        if (context.Features.Get<ITlsConnectionFeature>() != null)
        {
            await _next(context);
            return;
        }

        var sslDuplexPipe = CreateSslDuplexPipe(
            context.Transport,
            context.Features.Get<IMemoryPoolFeature>()?.MemoryPool ?? MemoryPool<byte>.Shared);
        var sslStream = sslDuplexPipe.Stream;

        var feature = new Core.Internal.TlsConnectionFeature(sslStream, context);
        // Set the mode if options were used. If the callback is used it will set the mode later.
        feature.AllowDelayedClientCertificateNegotation =
            _options?.ClientCertificateMode == ClientCertificateMode.DelayCertificate;
        context.Features.Set<ITlsConnectionFeature>(feature);
        context.Features.Set<ITlsHandshakeFeature>(feature);
        context.Features.Set<ITlsApplicationProtocolFeature>(feature);
        context.Features.Set<ISslStreamFeature>(feature);
        context.Features.Set<SslStream>(sslStream); // Anti-pattern, but retain for back compat

        var metricsTagsFeature = context.Features.Get<IConnectionMetricsTagsFeature>();
        var metricsContext = context.Features.GetRequiredFeature<IConnectionMetricsContextFeature>().MetricsContext;
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            using var cancellationTokenSource = _ctsPool.Rent();
            cancellationTokenSource.CancelAfter(_handshakeTimeout);

            if (_tlsCallbackOptions is null)
            {
                await DoOptionsBasedHandshakeAsync(context, sslStream, feature, cancellationTokenSource.Token);
            }
            else
            {
                var state = (this, context, feature, metricsContext);
                await sslStream.AuthenticateAsServerAsync(ServerOptionsCallback, state, cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException ex)
        {
            RecordHandshakeFailed(_metrics, startTimestamp, Stopwatch.GetTimestamp(), metricsContext, metricsTagsFeature, ex);

            _logger.AuthenticationTimedOut();
            await sslStream.DisposeAsync();
            return;
        }
        catch (IOException ex)
        {
            RecordHandshakeFailed(_metrics, startTimestamp, Stopwatch.GetTimestamp(), metricsContext, metricsTagsFeature, ex);

            _logger.AuthenticationFailed(ex);
            await sslStream.DisposeAsync();
            return;
        }
        catch (AuthenticationException ex)
        {
            RecordHandshakeFailed(_metrics, startTimestamp, Stopwatch.GetTimestamp(), metricsContext, metricsTagsFeature, ex);

            _logger.AuthenticationFailed(ex);
            await sslStream.DisposeAsync();
            return;
        }

        KestrelEventSource.Log.TlsHandshakeStop(context, feature);
        _metrics.TlsHandshakeStop(metricsContext, startTimestamp, Stopwatch.GetTimestamp(), protocol: sslStream.SslProtocol);

        _logger.HttpsConnectionEstablished(context.ConnectionId, sslStream.SslProtocol);

        if (metricsTagsFeature != null)
        {
            if (KestrelMetrics.TryGetHandshakeProtocol(sslStream.SslProtocol, out var protocolName, out var protocolVersion))
            {
                // "tls" is considered the default protocol name and isn't explicitly recorded.
                if (protocolName != "tls")
                {
                    metricsTagsFeature.Tags.Add(new KeyValuePair<string, object?>("tls.protocol.name", protocolName));
                }
                metricsTagsFeature.Tags.Add(new KeyValuePair<string, object?>("tls.protocol.version", protocolVersion));
            }
        }

        var originalTransport = context.Transport;

        try
        {
            context.Transport = sslDuplexPipe;

            // Disposing the stream will dispose the sslDuplexPipe
            await using (sslStream)
            await using (sslDuplexPipe)
            {
                await _next(context);
                // Dispose the inner stream (SslDuplexPipe) before disposing the SslStream
                // as the duplex pipe can hit an ODE as it still may be writing.
            }
        }
        finally
        {
            // Restore the original so that it gets closed appropriately
            context.Transport = originalTransport;
        }

        static void RecordHandshakeFailed(KestrelMetrics metrics, long startTimestamp, long currentTimestamp, ConnectionMetricsContext metricsContext, IConnectionMetricsTagsFeature? metricsTagsFeature, Exception ex)
        {
            KestrelEventSource.Log.TlsHandshakeFailed(metricsContext.ConnectionContext.ConnectionId);
            KestrelEventSource.Log.TlsHandshakeStop(metricsContext.ConnectionContext, null);

            KestrelMetrics.AddConnectionEndReason(metricsTagsFeature, ConnectionEndReason.TlsHandshakeFailed);
            metrics.TlsHandshakeStop(metricsContext, startTimestamp, currentTimestamp, exception: ex);
        }
    }

    // This logic is replicated from https://github.com/dotnet/runtime/blob/02b24db7cada5d5806c5cc513e61e44fb2a41944/src/libraries/System.Net.Security/src/System/Net/Security/SecureChannel.cs#L195-L262
    // but with public APIs
    private X509Certificate2 LocateCertificateWithPrivateKey(X509Certificate2 certificate)
    {
        Debug.Assert(!certificate.HasPrivateKey, "This should only be called with certificates that don't have a private key");

        _logger.LocatingCertWithPrivateKey(certificate);

        X509Store? OpenStore(StoreLocation storeLocation)
        {
            try
            {
                var store = new X509Store(StoreName.My, storeLocation);
                store.Open(OpenFlags.ReadOnly);
                return store;
            }
            catch (Exception exception) when (exception is CryptographicException || exception is SecurityException)
            {
                _logger.FailedToOpenStore(storeLocation, exception);
                return null;
            }
        }

        try
        {
            var store = OpenStore(StoreLocation.LocalMachine);

            if (store != null)
            {
                using (store)
                {
                    var certs = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, validOnly: false);

                    if (certs.Count > 0 && certs[0].HasPrivateKey)
                    {
                        _logger.FoundCertWithPrivateKey(certs[0], StoreLocation.LocalMachine);
                        return certs[0];
                    }
                }
            }

            store = OpenStore(StoreLocation.CurrentUser);

            if (store != null)
            {
                using (store)
                {
                    var certs = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, validOnly: false);

                    if (certs.Count > 0 && certs[0].HasPrivateKey)
                    {
                        _logger.FoundCertWithPrivateKey(certs[0], StoreLocation.CurrentUser);
                        return certs[0];
                    }
                }
            }
        }
        catch (CryptographicException ex)
        {
            // Log as debug since this error is expected an swallowed
            _logger.FailedToFindCertificateInStore(ex);
        }

        // Return the cert, and it will fail later
        return certificate;
    }

    private Task DoOptionsBasedHandshakeAsync(ConnectionContext context, SslStream sslStream, Core.Internal.TlsConnectionFeature feature, CancellationToken cancellationToken)
    {
        Debug.Assert(_options != null, "Middleware must be created with options.");

        // Adapt to the SslStream signature
        ServerCertificateSelectionCallback? selector = null;
        if (_serverCertificateSelector != null)
        {
            selector = (sender, name) =>
            {
                feature.HostName = name ?? string.Empty;
                var cert = _serverCertificateSelector(context, name);
                if (cert != null)
                {
                    EnsureCertificateIsAllowedForServerAuth(cert, _logger);
                }
                return cert!;
            };
        }

        var sslOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = _serverCertificate,
            ServerCertificateContext = _serverCertificateContext,
            ServerCertificateSelectionCallback = selector,
            ClientCertificateRequired = _options.ClientCertificateMode == ClientCertificateMode.AllowCertificate
                || _options.ClientCertificateMode == ClientCertificateMode.RequireCertificate,
            EnabledSslProtocols = _options.SslProtocols,
            CertificateRevocationCheckMode = _options.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
        };

        ConfigureAlpn(sslOptions, _httpProtocols);

        _options.OnAuthenticate?.Invoke(context, sslOptions);

        KestrelEventSource.Log.TlsHandshakeStart(context, sslOptions);
        _metrics.TlsHandshakeStart(context.Features.GetRequiredFeature<IConnectionMetricsContextFeature>().MetricsContext);

        return sslStream.AuthenticateAsServerAsync(sslOptions, cancellationToken);
    }

    internal static void ConfigureAlpn(SslServerAuthenticationOptions serverOptions, HttpProtocols httpProtocols)
    {
        serverOptions.ApplicationProtocols = new List<SslApplicationProtocol>();

        // This is order sensitive
        if ((httpProtocols & HttpProtocols.Http2) != 0)
        {
            serverOptions.ApplicationProtocols.Add(SslApplicationProtocol.Http2);
            // https://tools.ietf.org/html/rfc7540#section-9.2.1
            serverOptions.AllowRenegotiation = false;
        }

        if ((httpProtocols & HttpProtocols.Http1) != 0)
        {
            serverOptions.ApplicationProtocols.Add(SslApplicationProtocol.Http11);
        }
    }

    internal static bool RemoteCertificateValidationCallback(
        ClientCertificateMode clientCertificateMode,
        Func<X509Certificate2, X509Chain?, SslPolicyErrors, bool>? clientCertificateValidation,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (certificate == null)
        {
            return clientCertificateMode != ClientCertificateMode.RequireCertificate;
        }

        if (clientCertificateValidation == null)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                return false;
            }
        }

        var certificate2 = ConvertToX509Certificate2(certificate);
        if (certificate2 == null)
        {
            return false;
        }

        if (clientCertificateValidation != null)
        {
            if (!clientCertificateValidation(certificate2, chain, sslPolicyErrors))
            {
                return false;
            }
        }

        return true;
    }

    private bool RemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        Debug.Assert(_options != null, "Middleware must be created with options.");

        return RemoteCertificateValidationCallback(_options.ClientCertificateMode, _options.ClientCertificateValidation, certificate, chain, sslPolicyErrors);
    }

    private SslDuplexPipe CreateSslDuplexPipe(IDuplexPipe transport, MemoryPool<byte> memoryPool)
    {
        StreamPipeReaderOptions inputPipeOptions = new StreamPipeReaderOptions
        (
            pool: memoryPool,
            bufferSize: memoryPool.GetMinimumSegmentSize(),
            minimumReadSize: memoryPool.GetMinimumAllocSize(),
            leaveOpen: true,
            useZeroByteReads: true
        );

        var outputPipeOptions = new StreamPipeWriterOptions
        (
            pool: memoryPool,
            leaveOpen: true
        );

        return new SslDuplexPipe(transport, inputPipeOptions, outputPipeOptions, _sslStreamFactory);
    }

    private static async ValueTask<SslServerAuthenticationOptions> ServerOptionsCallback(SslStream sslStream, SslClientHelloInfo clientHelloInfo, object? state, CancellationToken cancellationToken)
    {
        var (middleware, context, feature, metricsContext) = (ValueTuple<HttpsConnectionMiddleware, ConnectionContext, Core.Internal.TlsConnectionFeature, ConnectionMetricsContext>)state!;

        feature.HostName = clientHelloInfo.ServerName;
        context.Features.Set(sslStream);
        var callbackContext = new TlsHandshakeCallbackContext
        {
            Connection = context,
            SslStream = sslStream,
            State = middleware._tlsCallbackOptionsState,
            CancellationToken = cancellationToken,
            ClientHelloInfo = clientHelloInfo,
        };
        var sslOptions = await middleware._tlsCallbackOptions!(callbackContext);
        feature.AllowDelayedClientCertificateNegotation = callbackContext.AllowDelayedClientCertificateNegotation;

        // The callback didn't set ALPN so we will.
        if (sslOptions.ApplicationProtocols == null)
        {
            ConfigureAlpn(sslOptions, middleware._httpProtocols);
        }

        KestrelEventSource.Log.TlsHandshakeStart(context, sslOptions);
        middleware._metrics.TlsHandshakeStart(metricsContext);

        return sslOptions;
    }

    internal static void EnsureCertificateIsAllowedForServerAuth(X509Certificate2 certificate, ILogger<HttpsConnectionMiddleware> logger)
    {
        if (!CertificateLoader.IsCertificateAllowedForServerAuth(certificate))
        {
            throw new InvalidOperationException(CoreStrings.FormatInvalidServerCertificateEku(certificate.Thumbprint));
        }
        else if (!CertificateLoader.DoesCertificateHaveASubjectAlternativeName(certificate))
        {
            logger.NoSubjectAlternativeName(certificate.Thumbprint);
        }
    }

    private static X509Certificate2? ConvertToX509Certificate2(X509Certificate? certificate)
    {
        if (certificate == null)
        {
            return null;
        }

        if (certificate is X509Certificate2 cert2)
        {
            return cert2;
        }

        return new X509Certificate2(certificate);
    }

    internal static HttpProtocols ValidateAndNormalizeHttpProtocols(HttpProtocols httpProtocols, ILogger<HttpsConnectionMiddleware> logger)
    {
        // This configuration will always fail per-request, preemptively fail it here. See HttpConnection.SelectProtocol().
        if (httpProtocols == HttpProtocols.Http2)
        {
            if (!TlsAlpn.IsSupported)
            {
                throw new NotSupportedException(CoreStrings.Http2NoTlsAlpn);
            }
            else if (_isWindowsVersionIncompatibleWithHttp2)
            {
                throw new NotSupportedException(CoreStrings.Http2NoTlsWin81);
            }
        }
        else if (httpProtocols == HttpProtocols.Http1AndHttp2 && _isWindowsVersionIncompatibleWithHttp2)
        {
            logger.Http2DefaultCiphersInsufficient();
            return HttpProtocols.Http1;
        }

        return httpProtocols;
    }

    private static bool IsWindowsVersionIncompatibleWithHttp2()
    {
        if (OperatingSystem.IsWindows())
        {
            var enableHttp2OnWindows81 = AppContext.TryGetSwitch(EnableWindows81Http2, out var enabled) && enabled;
            if (Environment.OSVersion.Version < new Version(6, 3) // Missing ALPN support
                                                                  // Win8.1 and 2012 R2 don't support the right cipher configuration by default.
                || (Environment.OSVersion.Version < new Version(10, 0) && !enableHttp2OnWindows81))
            {
                return true;
            }
        }

        return false;
    }

    internal static SslServerAuthenticationOptions CreateHttp3Options(HttpsConnectionAdapterOptions httpsOptions, ILogger<HttpsConnectionMiddleware> logger)
    {
        if (httpsOptions.OnAuthenticate != null)
        {
            throw new NotSupportedException($"The {nameof(HttpsConnectionAdapterOptions.OnAuthenticate)} callback is not supported with HTTP/3.");
        }

        // TODO Set other relevant values on options
        var sslServerAuthenticationOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = httpsOptions.ServerCertificate,
            ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http3 },
            CertificateRevocationCheckMode = httpsOptions.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
        };

        if (httpsOptions.ServerCertificateSelector != null)
        {
            // We can't set both
            sslServerAuthenticationOptions.ServerCertificate = null;
            sslServerAuthenticationOptions.ServerCertificateSelectionCallback = (sender, host) =>
            {
                // There is no ConnectionContext available durring the QUIC handshake.
                var cert = httpsOptions.ServerCertificateSelector(null, host);
                if (cert != null)
                {
                    EnsureCertificateIsAllowedForServerAuth(cert, logger);
                }
                return cert!;
            };
        }

        // DelayCertificate is prohibited by the HTTP/2 and HTTP/3 protocols, ignore it here.
        if (httpsOptions.ClientCertificateMode == ClientCertificateMode.AllowCertificate
                || httpsOptions.ClientCertificateMode == ClientCertificateMode.RequireCertificate)
        {
            sslServerAuthenticationOptions.ClientCertificateRequired = true; // We have to set this to prompt the client for a cert.
                                                                             // For AllowCertificate we override the missing cert error in RemoteCertificateValidationCallback,
                                                                             // except QuicListener doesn't call the callback for missing certs https://github.com/dotnet/runtime/issues/57308.
            sslServerAuthenticationOptions.RemoteCertificateValidationCallback
                = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
                    RemoteCertificateValidationCallback(httpsOptions.ClientCertificateMode, httpsOptions.ClientCertificateValidation, certificate, chain, sslPolicyErrors);
        }

        return sslServerAuthenticationOptions;
    }
}

internal static partial class HttpsConnectionMiddlewareLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Failed to authenticate HTTPS connection.", EventName = "AuthenticationFailed")]
    public static partial void AuthenticationFailed(this ILogger<HttpsConnectionMiddleware> logger, Exception exception);

    [LoggerMessage(2, LogLevel.Debug, "Authentication of the HTTPS connection timed out.", EventName = "AuthenticationTimedOut")]
    public static partial void AuthenticationTimedOut(this ILogger<HttpsConnectionMiddleware> logger);

    [LoggerMessage(3, LogLevel.Debug, "Connection {ConnectionId} established using the following protocol: {Protocol}", EventName = "HttpsConnectionEstablished")]
    public static partial void HttpsConnectionEstablished(this ILogger<HttpsConnectionMiddleware> logger, string connectionId, SslProtocols protocol);

    [LoggerMessage(4, LogLevel.Information, "HTTP/2 over TLS is not supported on Windows versions older than Windows 10 and Windows Server 2016 due to incompatible ciphers or missing ALPN support. Falling back to HTTP/1.1 instead.",
        EventName = "Http2DefaultCiphersInsufficient")]
    public static partial void Http2DefaultCiphersInsufficient(this ILogger<HttpsConnectionMiddleware> logger);

    [LoggerMessage(5, LogLevel.Debug, "Searching for certificate with private key and thumbprint {Thumbprint} in the certificate store.", EventName = "LocateCertWithPrivateKey")]
    private static partial void LocatingCertWithPrivateKey(this ILogger<HttpsConnectionMiddleware> logger, string thumbPrint);

    public static void LocatingCertWithPrivateKey(this ILogger<HttpsConnectionMiddleware> logger, X509Certificate2 certificate) => LocatingCertWithPrivateKey(logger, certificate.Thumbprint);

    [LoggerMessage(6, LogLevel.Debug, "Found certificate with private key and thumbprint {Thumbprint} in certificate store {StoreName}.", EventName = "FoundCertWithPrivateKey")]
    public static partial void FoundCertWithPrivateKey(this ILogger<HttpsConnectionMiddleware> logger, string thumbprint, string? storeName);

    public static void FoundCertWithPrivateKey(this ILogger<HttpsConnectionMiddleware> logger, X509Certificate2 certificate, StoreLocation storeLocation)
    {
        var storeLocationString = storeLocation == StoreLocation.LocalMachine ? nameof(StoreLocation.LocalMachine) : nameof(StoreLocation.CurrentUser);
        FoundCertWithPrivateKey(logger, certificate.Thumbprint, storeLocationString);
    }

    [LoggerMessage(7, LogLevel.Debug, "Failure to locate certificate from store.", EventName = "FailToLocateCertificate")]
    public static partial void FailedToFindCertificateInStore(this ILogger<HttpsConnectionMiddleware> logger, Exception exception);

    [LoggerMessage(8, LogLevel.Debug, "Failed to open certificate store {StoreName}.", EventName = "FailToOpenStore")]
    public static partial void FailedToOpenStore(this ILogger<HttpsConnectionMiddleware> logger, string? storeName, Exception exception);

    [LoggerMessage(9, LogLevel.Information, "Certificate with thumbprint {Thumbprint} lacks the subjectAlternativeName (SAN) extension and may not be accepted by browsers.", EventName = "NoSubjectAlternativeName")]
    public static partial void NoSubjectAlternativeName(this ILogger<HttpsConnectionMiddleware> logger, string thumbprint);

    public static void FailedToOpenStore(this ILogger<HttpsConnectionMiddleware> logger, StoreLocation storeLocation, Exception exception)
    {
        var storeLocationString = storeLocation == StoreLocation.LocalMachine ? nameof(StoreLocation.LocalMachine) : nameof(StoreLocation.CurrentUser);
        FailedToOpenStore(logger, storeLocationString, exception);
    }
}
