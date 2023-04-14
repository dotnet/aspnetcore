// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// An abstraction over the parts of <see cref="KestrelConfigurationLoader"/> that would prevent us from trimming TLS support
/// in `CreateSlimBuilder` scenarios.  Managed by <see cref="HttpsConfigurationService"/>.
/// </summary>
internal sealed class TlsConfigurationLoader
{
    private readonly ICertificateConfigLoader _certificateConfigLoader;
    private readonly string _applicationName;
    private readonly ILogger<KestrelServer> _serverLogger;
    private readonly ILogger<HttpsConnectionMiddleware> _httpsLogger;

    public TlsConfigurationLoader(
        IHostEnvironment hostEnvironment,
        ILogger<KestrelServer> serverLogger,
        ILogger<HttpsConnectionMiddleware> httpsLogger)
    {
        _certificateConfigLoader = new CertificateConfigLoader(hostEnvironment, serverLogger);
        _applicationName = hostEnvironment.ApplicationName;
        _serverLogger = serverLogger;
        _httpsLogger = httpsLogger;
    }

    /// <summary>
    /// Applies various configuration settings to <paramref name="httpsOptions"/> and <paramref name="endpoint"/>.
    /// </summary>
    public void ApplyHttpsConfiguration(
        HttpsConnectionAdapterOptions httpsOptions,
        EndpointConfig endpoint,
        KestrelServerOptions serverOptions,
        CertificateConfig? defaultCertificateConfig,
        ConfigurationReader configurationReader)
    {
        serverOptions.ApplyHttpsDefaults(httpsOptions);

        if (endpoint.SslProtocols.HasValue)
        {
            httpsOptions.SslProtocols = endpoint.SslProtocols.Value;
        }
        else
        {
            // Ensure endpoint is reloaded if it used the default protocol and the SslProtocols changed.
            endpoint.SslProtocols = configurationReader.EndpointDefaults.SslProtocols;
        }

        if (endpoint.ClientCertificateMode.HasValue)
        {
            httpsOptions.ClientCertificateMode = endpoint.ClientCertificateMode.Value;
        }
        else
        {
            // Ensure endpoint is reloaded if it used the default mode and the ClientCertificateMode changed.
            endpoint.ClientCertificateMode = configurationReader.EndpointDefaults.ClientCertificateMode;
        }

        // A cert specified directly on the endpoint overrides any defaults.
        var (serverCert, fullChain) = _certificateConfigLoader.LoadCertificate(endpoint.Certificate, endpoint.Name);
        httpsOptions.ServerCertificate = serverCert ?? httpsOptions.ServerCertificate;
        httpsOptions.ServerCertificateChain = fullChain ?? httpsOptions.ServerCertificateChain;

        if (!httpsOptions.HasServerCertificateOrSelector)
        {
            // Fallback
            serverOptions.ApplyDefaultCertificate(httpsOptions);

            // Ensure endpoint is reloaded if it used the default certificate and the certificate changed.
            endpoint.Certificate = defaultCertificateConfig;
        }
    }

    /// <summary>
    /// Calls an appropriate overload of <see cref="Microsoft.AspNetCore.Hosting.ListenOptionsHttpsExtensions.UseHttps(ListenOptions)"/>
    /// on <paramref name="listenOptions"/>, with or without SNI, according to how <paramref name="endpoint"/> is configured.
    /// </summary>
    /// <returns>Updated <see cref="ListenOptions"/> for convenient chaining.</returns>
    public ListenOptions UseHttpsWithSni(
        ListenOptions listenOptions,
        HttpsConnectionAdapterOptions httpsOptions,
        EndpointConfig endpoint)
    {
        if (listenOptions.IsTls)
        {
            return listenOptions;
        }

        if (endpoint.Sni.Count == 0)
        {
            if (!httpsOptions.HasServerCertificateOrSelector)
            {
                throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
            }

            return listenOptions.UseHttps(httpsOptions);
        }

        var sniOptionsSelector = new SniOptionsSelector(endpoint.Name, endpoint.Sni, _certificateConfigLoader,
            httpsOptions, listenOptions.Protocols, _httpsLogger);
        var tlsCallbackOptions = new TlsHandshakeCallbackOptions()
        {
            OnConnection = SniOptionsSelector.OptionsCallback,
            HandshakeTimeout = httpsOptions.HandshakeTimeout,
            OnConnectionState = sniOptionsSelector,
        };

        return listenOptions.UseHttps(tlsCallbackOptions);
    }

    /// <summary>
    /// Retrieves the default or, failing that, developer certificate from <paramref name="configurationReader"/>.
    /// </summary>
    public CertificateAndConfig? LoadDefaultCertificate(ConfigurationReader configurationReader)
    {
        if (configurationReader.Certificates.TryGetValue("Default", out var defaultCertConfig))
        {
            var (defaultCert, _ /* cert chain */) = _certificateConfigLoader.LoadCertificate(defaultCertConfig, "Default");
            if (defaultCert != null)
            {
                return new CertificateAndConfig(defaultCert, defaultCertConfig);
            }
        }
        else if (FindDeveloperCertificateFile(configurationReader) is CertificateAndConfig pair)
        {
            _serverLogger.LocatedDevelopmentCertificate(pair.Certificate);
            return pair;
        }

        return null;
    }

    private CertificateAndConfig? FindDeveloperCertificateFile(ConfigurationReader configurationReader)
    {
        string? certificatePath = null;
        if (configurationReader.Certificates.TryGetValue("Development", out var certificateConfig) &&
            certificateConfig.Path == null &&
            certificateConfig.Password != null &&
            TryGetCertificatePath(_applicationName, out certificatePath) &&
            File.Exists(certificatePath))
        {
            try
            {
                var certificate = new X509Certificate2(certificatePath, certificateConfig.Password);

                if (IsDevelopmentCertificate(certificate))
                {
                    return new CertificateAndConfig(certificate, certificateConfig);
                }
            }
            catch (CryptographicException)
            {
                _serverLogger.FailedToLoadDevelopmentCertificate(certificatePath);
            }
        }
        else if (!string.IsNullOrEmpty(certificatePath))
        {
            _serverLogger.FailedToLocateDevelopmentCertificateFile(certificatePath);
        }

        return null;
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

    private static bool TryGetCertificatePath(string applicationName, [NotNullWhen(true)] out string? path)
    {
        // See https://github.com/aspnet/Hosting/issues/1294
        var appData = Environment.GetEnvironmentVariable("APPDATA");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var basePath = appData != null ? Path.Combine(appData, "ASP.NET", "https") : null;
        basePath = basePath ?? (home != null ? Path.Combine(home, ".aspnet", "https") : null);
        path = basePath != null ? Path.Combine(basePath, $"{applicationName}.pfx") : null;
        return path != null;
    }
}
