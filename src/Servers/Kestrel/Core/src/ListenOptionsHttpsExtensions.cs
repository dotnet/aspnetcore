// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

        var options = new HttpsConnectionAdapterOptions();
        listenOptions.KestrelServerOptions.ApplyHttpsDefaults(options);
        configureOptions(options);
        listenOptions.KestrelServerOptions.ApplyDefaultCert(options);

        if (options.ServerCertificate == null && options.ServerCertificateSelector == null)
        {
            throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
        }

        return listenOptions.UseHttps(options);
    }

    // Use Https if a default cert is available
    internal static bool TryUseHttps(this ListenOptions listenOptions)
    {
        var options = new HttpsConnectionAdapterOptions();
        listenOptions.KestrelServerOptions.ApplyHttpsDefaults(options);
        listenOptions.KestrelServerOptions.ApplyDefaultCert(options);

        if (options.ServerCertificate == null && options.ServerCertificateSelector == null)
        {
            return false;
        }

        listenOptions.UseHttps(options);
        return true;
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
        var loggerFactory = listenOptions.KestrelServerOptions?.ApplicationServices.GetRequiredService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

        listenOptions.IsTls = true;
        listenOptions.HttpsOptions = httpsOptions;

        listenOptions.Use(next =>
        {
            // Set the list of protocols from listen options
            httpsOptions.HttpProtocols = listenOptions.Protocols;
            var middleware = new HttpsConnectionMiddleware(next, httpsOptions, loggerFactory);
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

        var loggerFactory = listenOptions.KestrelServerOptions?.ApplicationServices.GetRequiredService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

        listenOptions.IsTls = true;
        listenOptions.HttpsCallbackOptions = callbackOptions;

        listenOptions.Use(next =>
        {
            // Set the list of protocols from listen options.
            // Set it inside Use delegate so Protocols and UseHttps can be called out of order.
            callbackOptions.HttpProtocols = listenOptions.Protocols;

            var middleware = new HttpsConnectionMiddleware(next, callbackOptions, loggerFactory);
            return middleware.OnConnectionAsync;
        });

        return listenOptions;
    }
}
