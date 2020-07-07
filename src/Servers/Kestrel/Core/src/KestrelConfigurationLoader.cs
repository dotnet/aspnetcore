// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelConfigurationLoader
    {
        private bool _loaded = false;

        internal KestrelConfigurationLoader(KestrelServerOptions options, IConfiguration configuration, bool reloadOnChange)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ReloadOnChange = reloadOnChange;

            ConfigurationReader = new ConfigurationReader(configuration);
        }

        public KestrelServerOptions Options { get; }
        public IConfiguration Configuration { get; internal set; }

        /// <summary>
        /// If <see langword="true" />, Kestrel will dynamically update endpoint bindings when configuration changes.
        /// This will only reload endpoints defined in the "Endpoints" section of your Kestrel configuration. Endpoints defined in code will not be reloaded.
        /// </summary>
        internal bool ReloadOnChange { get; }

        private ConfigurationReader ConfigurationReader { get; set; }

        private IDictionary<string, Action<EndpointConfiguration>> EndpointConfigurations { get; }
            = new Dictionary<string, Action<EndpointConfiguration>>(0, StringComparer.OrdinalIgnoreCase);

        // Actions that will be delayed until Load so that they aren't applied if the configuration loader is replaced.
        private IList<Action> EndpointsToAdd { get; } = new List<Action>();

        private CertificateConfig DefaultCertificateConfig { get; set; }

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
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

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
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

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
            if (socketPath == null)
            {
                throw new ArgumentNullException(nameof(socketPath));
            }
            if (socketPath.Length == 0 || socketPath[0] != '/')
            {
                throw new ArgumentException(CoreStrings.UnixSocketPathMustBeAbsolute, nameof(socketPath));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EndpointsToAdd.Add(() =>
            {
                Options.ListenHandle(handle, configure);
            });

            return this;
        }

        // Called from ApplyEndpointDefaults so it applies to even explicit Listen endpoints.
        // Does not require a call to Load.
        internal void ApplyConfigurationDefaults(ListenOptions listenOptions)
        {
            var defaults = ConfigurationReader.EndpointDefaults;

            if (defaults.Protocols.HasValue)
            {
                listenOptions.Protocols = defaults.Protocols.Value;
            }
        }

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

            LoadDefaultCert(ConfigurationReader);

            foreach (var endpoint in ConfigurationReader.Endpoints)
            {
                var listenOptions = AddressBinder.ParseAddress(endpoint.Url, out var https);

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
                ServerOptionsSelectionCallback serverOptionsCallback = null;

                if (https)
                {
                    httpsOptions.SslProtocols = ConfigurationReader.EndpointDefaults.SslProtocols ?? SslProtocols.None;
                    httpsOptions.ClientCertificateMode = ConfigurationReader.EndpointDefaults.ClientCertificateMode ?? ClientCertificateMode.NoCertificate;

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
                        // Ensure endpoint is reloaded if it used the default protocol and the ClientCertificateMode changed.
                        endpoint.ClientCertificateMode = ConfigurationReader.EndpointDefaults.ClientCertificateMode;
                    }

                    // Specified
                    if (endpoint.SNI.Count == 0)
                    {
                        LoadEndpointOrDefaultCertificate(httpsOptions, endpoint);
                    }
                    else
                    {
                        serverOptionsCallback = (stream, clientHelloInfo, state, cancellationToken) =>
                            SniCallback(httpsOptions, listenOptions.Protocols, endpoint, clientHelloInfo);
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
                    if (serverOptionsCallback is null)
                    {
                        if (httpsOptions.ServerCertificate == null && httpsOptions.ServerCertificateSelector == null)
                        {
                            throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
                        }

                        listenOptions.UseHttps(httpsOptions);
                    }
                    else
                    {
                        listenOptions.UseHttps(serverOptionsCallback);
                    }
                }

                listenOptions.EndpointConfig = endpoint;

                endpointsToStart.Add(listenOptions);
                Options.ConfigurationBackedListenOptions.Add(listenOptions);
            }

            return (endpointsToStop, endpointsToStart);
        }

        private ValueTask<SslServerAuthenticationOptions> SniCallback(
            HttpsConnectionAdapterOptions httpsOptions,
            HttpProtocols endpointDefaultHttpProtocols,
            EndpointConfig endpoint,
            SslClientHelloInfo clientHelloInfo)
        {
            const string wildcardHost = "*";
            const string wildcardPrefix = "*.";

            var sslServerOptions = new SslServerAuthenticationOptions();
            var serverName = clientHelloInfo.ServerName;
            SniConfig sniConfig = null;

            if (!string.IsNullOrEmpty(serverName))
            {
                foreach (var (name, sniConfigCandidate) in endpoint.SNI)
                {
                    if (name.Equals(serverName, StringComparison.OrdinalIgnoreCase))
                    {
                        sniConfig = sniConfigCandidate;
                        break;
                    }
                    // Note that we only slice off the `*`. We want to match the leading `.` also.
                    else if (name.StartsWith(wildcardPrefix, StringComparison.Ordinal)
                        && serverName.EndsWith(name.Substring(wildcardHost.Length), StringComparison.OrdinalIgnoreCase)
                        && name.Length > (sniConfig?.Name.Length ?? 0))
                    {
                        sniConfig = sniConfigCandidate;
                    }
                    else if (name.Equals(wildcardHost, StringComparison.Ordinal))
                    {
                        sniConfig ??= sniConfigCandidate;
                    }
                }
            }

            sslServerOptions.ServerCertificate = LoadCertificate(sniConfig?.Certificate, endpoint.Name) ?? LoadEndpointOrDefaultCertificate(httpsOptions, endpoint);

            if (sslServerOptions.ServerCertificate is null)
            {
                throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
            }

            sslServerOptions.EnabledSslProtocols = sniConfig?.SslProtocols ?? httpsOptions.SslProtocols;
            HttpsConnectionMiddleware.ConfigureAlpn(sslServerOptions, sniConfig?.Protocols ?? endpointDefaultHttpProtocols);

            var clientCertificateMode = sniConfig?.ClientCertificateMode ?? httpsOptions.ClientCertificateMode;

            if (clientCertificateMode != ClientCertificateMode.NoCertificate)
            {
                sslServerOptions.ClientCertificateRequired = true;
                sslServerOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    HttpsConnectionMiddleware.RemoteCertificateValidationCallback(clientCertificateMode, httpsOptions.ClientCertificateValidation, certificate, chain, sslPolicyErrors);
            }

            return new ValueTask<SslServerAuthenticationOptions>(sslServerOptions);
        }

        private X509Certificate2 LoadEndpointOrDefaultCertificate(HttpsConnectionAdapterOptions httpsOptions, EndpointConfig endpoint)
        {
            // Specified
            httpsOptions.ServerCertificate = LoadCertificate(endpoint.Certificate, endpoint.Name)
                ?? httpsOptions.ServerCertificate;

            if (httpsOptions.ServerCertificate == null && httpsOptions.ServerCertificateSelector == null)
            {
                // Fallback
                Options.ApplyDefaultCert(httpsOptions);

                // Ensure endpoint is reloaded if it used the default certificate and the certificate changed.
                endpoint.Certificate = DefaultCertificateConfig;
            }

            return httpsOptions.ServerCertificate;
        }

        private void LoadDefaultCert(ConfigurationReader configReader)
        {
            if (configReader.Certificates.TryGetValue("Default", out var defaultCertConfig))
            {
                var defaultCert = LoadCertificate(defaultCertConfig, "Default");
                if (defaultCert != null)
                {
                    DefaultCertificateConfig = defaultCertConfig;
                    Options.DefaultCertificate = defaultCert;
                }
            }
            else
            {
                var logger = Options.ApplicationServices.GetRequiredService<ILogger<KestrelServer>>();
                var (certificate, certificateConfig) = FindDeveloperCertificateFile(configReader, logger);
                if (certificate != null)
                {
                    logger.LocatedDevelopmentCertificate(certificate);
                    DefaultCertificateConfig = certificateConfig;
                    Options.DefaultCertificate = certificate;
                }
            }
        }

        private (X509Certificate2, CertificateConfig) FindDeveloperCertificateFile(ConfigurationReader configReader, ILogger<KestrelServer> logger)
        {
            string certificatePath = null;
            try
            {
                if (configReader.Certificates.TryGetValue("Development", out var certificateConfig) &&
                    certificateConfig.Path == null &&
                    certificateConfig.Password != null &&
                    TryGetCertificatePath(out certificatePath) &&
                    File.Exists(certificatePath))
                {
                    var certificate = new X509Certificate2(certificatePath, certificateConfig.Password);

                    if (IsDevelopmentCertificate(certificate))
                    {
                        return (certificate, certificateConfig);
                    }
                }
                else if (!string.IsNullOrEmpty(certificatePath))
                {
                    logger.FailedToLocateDevelopmentCertificateFile(certificatePath);
                }
            }
            catch (CryptographicException)
            {
                logger.FailedToLoadDevelopmentCertificate(certificatePath);
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
                if (string.Equals(ext.Oid.Value, CertificateManager.AspNetHttpsOid, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetCertificatePath(out string path)
        {
            var hostingEnvironment = Options.ApplicationServices.GetRequiredService<IHostEnvironment>();
            var appName = hostingEnvironment.ApplicationName;

            // This will go away when we implement
            // https://github.com/aspnet/Hosting/issues/1294
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            var home = Environment.GetEnvironmentVariable("HOME");
            var basePath = appData != null ? Path.Combine(appData, "ASP.NET", "https") : null;
            basePath = basePath ?? (home != null ? Path.Combine(home, ".aspnet", "https") : null);
            path = basePath != null ? Path.Combine(basePath, $"{appName}.pfx") : null;
            return path != null;
        }

        private X509Certificate2 LoadCertificate(CertificateConfig certInfo, string endpointName)
        {
            if (certInfo is null)
            {
                return null;
            }

            var logger = Options.ApplicationServices.GetRequiredService<ILogger<KestrelConfigurationLoader>>();
            if (certInfo.IsFileCert && certInfo.IsStoreCert)
            {
                throw new InvalidOperationException(CoreStrings.FormatMultipleCertificateSources(endpointName));
            }
            else if (certInfo.IsFileCert)
            {
                var environment = Options.ApplicationServices.GetRequiredService<IHostEnvironment>();
                var certificatePath = Path.Combine(environment.ContentRootPath, certInfo.Path);
                if (certInfo.KeyPath != null)
                {
                    var certificateKeyPath = Path.Combine(environment.ContentRootPath, certInfo.KeyPath);
                    var certificate = GetCertificate(certificatePath);

                    if (certificate != null)
                    {
                        certificate = LoadCertificateKey(certificate, certificateKeyPath, certInfo.Password);
                    }
                    else
                    {
                        logger.FailedToLoadCertificate(certificateKeyPath);
                    }

                    if (certificate != null)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            return PersistKey(certificate);
                        }

                        return certificate;
                    }
                    else
                    {
                        logger.FailedToLoadCertificateKey(certificateKeyPath);
                    }

                    throw new InvalidOperationException(CoreStrings.InvalidPemKey);
                }

                return new X509Certificate2(Path.Combine(environment.ContentRootPath, certInfo.Path), certInfo.Password);
            }
            else if (certInfo.IsStoreCert)
            {
                return LoadFromStoreCert(certInfo);
            }
            return null;

            static X509Certificate2 PersistKey(X509Certificate2 fullCertificate)
            {
                // We need to force the key to be persisted.
                // See https://github.com/dotnet/runtime/issues/23749
                var certificateBytes = fullCertificate.Export(X509ContentType.Pkcs12, "");
                return new X509Certificate2(certificateBytes, "", X509KeyStorageFlags.DefaultKeySet);
            }

            static X509Certificate2 LoadCertificateKey(X509Certificate2 certificate, string keyPath, string password)
            {
                // OIDs for the certificate key types.
                const string RSAOid = "1.2.840.113549.1.1.1";
                const string DSAOid = "1.2.840.10040.4.1";
                const string ECDsaOid = "1.2.840.10045.2.1";

                var keyText = File.ReadAllText(keyPath);
                return certificate.PublicKey.Oid.Value switch
                {
                    RSAOid => AttachPemRSAKey(certificate, keyText, password),
                    ECDsaOid => AttachPemECDSAKey(certificate, keyText, password),
                    DSAOid => AttachPemDSAKey(certificate, keyText, password),
                    _ => throw new InvalidOperationException(string.Format(CoreStrings.UnrecognizedCertificateKeyOid, certificate.PublicKey.Oid.Value))
                };
            }

            static X509Certificate2 GetCertificate(string certificatePath)
            {
                if (X509Certificate2.GetCertContentType(certificatePath) == X509ContentType.Cert)
                {
                    return new X509Certificate2(certificatePath);
                }

                return null;
            }
        }

        private static X509Certificate2 AttachPemRSAKey(X509Certificate2 certificate, string keyText, string password)
        {
            using var rsa = RSA.Create();
            if (password == null)
            {
                rsa.ImportFromPem(keyText);
            }
            else
            {
                rsa.ImportFromEncryptedPem(keyText, password);
            }

            return certificate.CopyWithPrivateKey(rsa);
        }

        private static X509Certificate2 AttachPemDSAKey(X509Certificate2 certificate, string keyText, string password)
        {
            using var dsa = DSA.Create();
            if (password == null)
            {
                dsa.ImportFromPem(keyText);
            }
            else
            {
                dsa.ImportFromEncryptedPem(keyText, password);
            }

            return certificate.CopyWithPrivateKey(dsa);
        }

        private static X509Certificate2 AttachPemECDSAKey(X509Certificate2 certificate, string keyText, string password)
        {
            using var ecdsa = ECDsa.Create();
            if (password == null)
            {
                ecdsa.ImportFromPem(keyText);
            }
            else
            {
                ecdsa.ImportFromEncryptedPem(keyText, password);
            }

            return certificate.CopyWithPrivateKey(ecdsa);
        }

        private static X509Certificate2 LoadFromStoreCert(CertificateConfig certInfo)
        {
            var subject = certInfo.Subject;
            var storeName = string.IsNullOrEmpty(certInfo.Store) ? StoreName.My.ToString() : certInfo.Store;
            var location = certInfo.Location;
            var storeLocation = StoreLocation.CurrentUser;
            if (!string.IsNullOrEmpty(location))
            {
                storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), location, ignoreCase: true);
            }
            var allowInvalid = certInfo.AllowInvalid ?? false;

            return CertificateLoader.LoadFromStoreCert(subject, storeName, storeLocation, allowInvalid);
        }
    }
}
