// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Middleware;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods for <see cref="ListenOptions"/> that configure Kestrel to use HTTPS for a given endpoint.
/// </summary>
public static class ListenOptionsHttpsExtensions
{
    /// <summary>
    /// Configure Kestrel to use HTTPS with the default certificate if available.
    /// This will throw if no default certificate is configured.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions) => listenOptions.UseHttps(_ => { });

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="fileName">The name of a certificate file, relative to the directory that contains the application
    /// content files.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName)
    {
        var env = listenOptions.ApplicationServices.GetRequiredService<IHostEnvironment>();
        return listenOptions.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName)));
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="fileName">The name of a certificate file, relative to the directory that contains the application
    /// content files.</param>
    /// <param name="password">The password required to access the X.509 certificate data.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName, string? password)
    {
        var env = listenOptions.ApplicationServices.GetRequiredService<IHostEnvironment>();
        return listenOptions.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName), password));
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="fileName">The name of a certificate file, relative to the directory that contains the application content files.</param>
    /// <param name="password">The password required to access the X.509 certificate data.</param>
    /// <param name="configureOptions">An Action to configure the <see cref="HttpsConnectionAdapterOptions"/>.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName, string? password,
        Action<HttpsConnectionAdapterOptions> configureOptions)
    {
        var env = listenOptions.ApplicationServices.GetRequiredService<IHostEnvironment>();
        return listenOptions.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName), password), configureOptions);
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="storeName">The certificate store to load the certificate from.</param>
    /// <param name="subject">The subject name for the certificate to load.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject)
        => listenOptions.UseHttps(storeName, subject, allowInvalid: false);

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="storeName">The certificate store to load the certificate from.</param>
    /// <param name="subject">The subject name for the certificate to load.</param>
    /// <param name="allowInvalid">Indicates if invalid certificates should be considered, such as self-signed certificates.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject, bool allowInvalid)
        => listenOptions.UseHttps(storeName, subject, allowInvalid, StoreLocation.CurrentUser);

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="storeName">The certificate store to load the certificate from.</param>
    /// <param name="subject">The subject name for the certificate to load.</param>
    /// <param name="allowInvalid">Indicates if invalid certificates should be considered, such as self-signed certificates.</param>
    /// <param name="location">The store location to load the certificate from.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject, bool allowInvalid, StoreLocation location)
        => listenOptions.UseHttps(storeName, subject, allowInvalid, location, configureOptions: _ => { });

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="storeName">The certificate store to load the certificate from.</param>
    /// <param name="subject">The subject name for the certificate to load.</param>
    /// <param name="allowInvalid">Indicates if invalid certificates should be considered, such as self-signed certificates.</param>
    /// <param name="location">The store location to load the certificate from.</param>
    /// <param name="configureOptions">An Action to configure the <see cref="HttpsConnectionAdapterOptions"/>.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject, bool allowInvalid, StoreLocation location,
        Action<HttpsConnectionAdapterOptions> configureOptions)
    {
        return listenOptions.UseHttps(CertificateLoader.LoadFromStoreCert(subject, storeName.ToString(), location, allowInvalid), configureOptions);
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions"> The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="serverCertificate">The X.509 certificate.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, X509Certificate2 serverCertificate)
    {
        ArgumentNullException.ThrowIfNull(serverCertificate);

        return listenOptions.UseHttps(options =>
        {
            options.ServerCertificate = serverCertificate;
        });
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="serverCertificate">The X.509 certificate.</param>
    /// <param name="configureOptions">An Action to configure the <see cref="HttpsConnectionAdapterOptions"/>.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, X509Certificate2 serverCertificate,
        Action<HttpsConnectionAdapterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(serverCertificate);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return listenOptions.UseHttps(options =>
        {
            options.ServerCertificate = serverCertificate;
            configureOptions(options);
        });
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="configureOptions">An action to configure options for HTTPS.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, Action<HttpsConnectionAdapterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        // We consider calls to `UseHttps` to be a clear expression of user intent to pull in HTTPS configuration support
        listenOptions.KestrelServerOptions.EnableHttpsConfiguration();

        // If there's a configuration, load it so that the results will be available to ApplyDefaultCertificate
        listenOptions.KestrelServerOptions.ConfigurationLoader?.LoadInternal();

        var options = new HttpsConnectionAdapterOptions();
        listenOptions.KestrelServerOptions.ApplyHttpsDefaults(options);
        configureOptions(options);
        listenOptions.KestrelServerOptions.ApplyDefaultCertificate(options);

        if (!options.HasServerCertificateOrSelector)
        {
            throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
        }

        return listenOptions.UseHttps(options);
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS. This does not use default certificates or other defaults specified via config or
    /// <see cref="KestrelServerOptions.ConfigureHttpsDefaults(Action{HttpsConnectionAdapterOptions})"/>.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="httpsOptions">Options to configure HTTPS.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions)
    {
        var loggerFactory = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var metrics = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<KestrelMetrics>();

        listenOptions.IsTls = true;
        listenOptions.HttpsOptions = httpsOptions;

        listenOptions.Use(next =>
        {
            var middleware = new HttpsConnectionMiddleware(next, httpsOptions, listenOptions.Protocols, loggerFactory, metrics);
            return middleware.OnConnectionAsync;
        });

        return listenOptions;
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS. This does not use default certificates or other defaults specified via config or
    /// <see cref="KestrelServerOptions.ConfigureHttpsDefaults(Action{HttpsConnectionAdapterOptions})"/>.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="serverOptionsSelectionCallback">Callback to configure HTTPS options.</param>
    /// <param name="state">State for the <paramref name="serverOptionsSelectionCallback"/>.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, ServerOptionsSelectionCallback serverOptionsSelectionCallback, object state)
    {
        return listenOptions.UseHttps(serverOptionsSelectionCallback, state, HttpsConnectionAdapterOptions.DefaultHandshakeTimeout);
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS. This does not use default certificates or other defaults specified via config or
    /// <see cref="KestrelServerOptions.ConfigureHttpsDefaults(Action{HttpsConnectionAdapterOptions})"/>.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="serverOptionsSelectionCallback">Callback to configure HTTPS options.</param>
    /// <param name="state">State for the <paramref name="serverOptionsSelectionCallback"/>.</param>
    /// <param name="handshakeTimeout">Specifies the maximum amount of time allowed for the TLS/SSL handshake. This must be positive and finite.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, ServerOptionsSelectionCallback serverOptionsSelectionCallback, object state, TimeSpan handshakeTimeout)
    {
        return listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
        {
            OnConnection = context => serverOptionsSelectionCallback(context.SslStream, context.ClientHelloInfo, context.State, context.CancellationToken),
            HandshakeTimeout = handshakeTimeout,
            OnConnectionState = state,
        });
    }

    /// <summary>
    /// Configure Kestrel to use HTTPS. This does not use default certificates or other defaults specified via config or
    /// <see cref="KestrelServerOptions.ConfigureHttpsDefaults(Action{HttpsConnectionAdapterOptions})"/>.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="callbackOptions">Options for a per connection callback.</param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, TlsHandshakeCallbackOptions callbackOptions)
    {
        ArgumentNullException.ThrowIfNull(callbackOptions);

        if (callbackOptions.OnConnection is null)
        {
            throw new ArgumentException($"{nameof(TlsHandshakeCallbackOptions.OnConnection)} must not be null.");
        }

        var loggerFactory = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var metrics = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<KestrelMetrics>();

        listenOptions.IsTls = true;
        listenOptions.HttpsCallbackOptions = callbackOptions;

        listenOptions.Use(next =>
        {
            // Set the list of protocols from listen options.
            // Set it inside Use delegate so Protocols and UseHttps can be called out of order.
            callbackOptions.HttpProtocols = listenOptions.Protocols;

            var middleware = new HttpsConnectionMiddleware(next, callbackOptions, loggerFactory, metrics);
            return middleware.OnConnectionAsync;
        });

        return listenOptions;
    }

    // The Client Hello is typically sent in the first flight of the connection,
    // so a shorter timeout than the full handshake (10s default) is appropriate.
    internal static readonly TimeSpan DefaultTlsClientHelloListenerTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Adds a connection middleware that sniffs the TLS Client Hello message and invokes <paramref name="tlsClientHelloBytesCallback"/>
    /// with the raw bytes before the TLS handshake is performed.
    /// This must be called before <c>UseHttps()</c> so that the middleware runs prior to the TLS handshake.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="tlsClientHelloBytesCallback">
    /// The callback to invoke with the <see cref="ConnectionContext"/> and the raw TLS Client Hello bytes
    /// (still wrapped in the TLS record layer fragment).
    /// </param>
    /// <param name="timeout">
    /// The maximum time to wait for the TLS Client Hello message. Defaults to 8 seconds if not specified.
    /// </param>
    /// <remarks>
    /// Note that this timeout is additive with the TLS handshake timeout (default 10 seconds).
    /// A slow client could take up to the sum of both timeouts (e.g. 8 + 10 = 18 seconds by default)
    /// before the connection is aborted. Consider reducing each timeout accordingly
    /// (e.g. 5 seconds for the Client Hello and 5 seconds for the handshake) to keep the total time bounded.
    /// </remarks>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseTlsClientHelloListener(this ListenOptions listenOptions, Action<ConnectionContext, ReadOnlySequence<byte>> tlsClientHelloBytesCallback, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(listenOptions);
        ArgumentNullException.ThrowIfNull(tlsClientHelloBytesCallback);
        if (timeout.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout.Value, TimeSpan.Zero, nameof(timeout));
        }

        var effectiveTimeout = timeout ?? DefaultTlsClientHelloListenerTimeout;
        var tlsListener = new TlsListener(tlsClientHelloBytesCallback);
        var ctsPool = new CancellationTokenSourcePool();

        listenOptions.Use(next =>
        {
            return async context =>
            {
                using (var timeoutCts = ctsPool.Rent())
                {
                    timeoutCts.CancelAfter(effectiveTimeout);
                    await tlsListener.OnTlsClientHelloAsync(context, timeoutCts.Token);
                }

                await next(context);
            };
        });

        return listenOptions;
    }
}
