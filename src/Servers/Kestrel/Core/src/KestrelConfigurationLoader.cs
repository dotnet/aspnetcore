// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

        internal KestrelConfigurationLoader(KestrelServerOptions options, IConfiguration configuration)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ConfigurationReader = new ConfigurationReader(Configuration);
        }

        public KestrelServerOptions Options { get; }
        public IConfiguration Configuration { get; }
        internal ConfigurationReader ConfigurationReader { get; }
        private IDictionary<string, Action<EndpointConfiguration>> EndpointConfigurations { get; }
            = new Dictionary<string, Action<EndpointConfiguration>>(0, StringComparer.OrdinalIgnoreCase);
        // Actions that will be delayed until Load so that they aren't applied if the configuration loader is replaced.
        private IList<Action> EndpointsToAdd { get; } = new List<Action>();

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

            Options.Latin1RequestHeaders = ConfigurationReader.Latin1RequestHeaders;

            LoadDefaultCert(ConfigurationReader);

            foreach (var endpoint in ConfigurationReader.Endpoints)
            {
                var listenOptions = AddressBinder.ParseAddress(endpoint.Url, out var https);
                Options.ApplyEndpointDefaults(listenOptions);

                if (endpoint.Protocols.HasValue)
                {
                    listenOptions.Protocols = endpoint.Protocols.Value;
                }

                // Compare to UseHttps(httpsOptions => { })
                var httpsOptions = new HttpsConnectionAdapterOptions();
                if (https)
                {
                    // Defaults
                    Options.ApplyHttpsDefaults(httpsOptions);

                    // Specified
                    httpsOptions.ServerCertificate = LoadCertificate(endpoint.Certificate, endpoint.Name)
                        ?? httpsOptions.ServerCertificate;

                    // Fallback
                    Options.ApplyDefaultCert(httpsOptions);
                }

                if (EndpointConfigurations.TryGetValue(endpoint.Name, out var configureEndpoint))
                {
                    var endpointConfig = new EndpointConfiguration(https, listenOptions, httpsOptions, endpoint.ConfigSection);
                    configureEndpoint(endpointConfig);
                }

                // EndpointDefaults or configureEndpoint may have added an https adapter.
                if (https && !listenOptions.IsTls)
                {
                    if (httpsOptions.ServerCertificate == null && httpsOptions.ServerCertificateSelector == null)
                    {
                        throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
                    }

                    listenOptions.UseHttps(httpsOptions);
                }

                Options.ListenOptions.Add(listenOptions);
            }

            foreach (var action in EndpointsToAdd)
            {
                action();
            }
        }

        private void LoadDefaultCert(ConfigurationReader configReader)
        {
            if (configReader.Certificates.TryGetValue("Default", out var defaultCertConfig))
            {
                var defaultCert = LoadCertificate(defaultCertConfig, "Default");
                if (defaultCert != null)
                {
                    Options.DefaultCertificate = defaultCert;
                }
            }
            else
            {
                var logger = Options.ApplicationServices.GetRequiredService<ILogger<KestrelServer>>();
                var certificate = FindDeveloperCertificateFile(configReader, logger);
                if (certificate != null)
                {
                    logger.LocatedDevelopmentCertificate(certificate);
                    Options.DefaultCertificate = certificate;
                }
            }
        }

        private X509Certificate2 FindDeveloperCertificateFile(ConfigurationReader configReader, ILogger<KestrelServer> logger)
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
                    return IsDevelopmentCertificate(certificate) ? certificate : null;
                }
                else if (!File.Exists(certificatePath))
                {
                    logger.FailedToLocateDevelopmentCertificateFile(certificatePath);
                }
            }
            catch (CryptographicException)
            {
                logger.FailedToLoadDevelopmentCertificate(certificatePath);
            }

            return null;
        }

        private bool IsDevelopmentCertificate(X509Certificate2 certificate)
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
            if (certInfo.IsFileCert && certInfo.IsStoreCert)
            {
                throw new InvalidOperationException(CoreStrings.FormatMultipleCertificateSources(endpointName));
            }
            else if (certInfo.IsFileCert)
            {
                var env = Options.ApplicationServices.GetRequiredService<IHostEnvironment>();
                return new X509Certificate2(Path.Combine(env.ContentRootPath, certInfo.Path), certInfo.Password);
            }
            else if (certInfo.IsStoreCert)
            {
                return LoadFromStoreCert(certInfo);
            }
            return null;
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
