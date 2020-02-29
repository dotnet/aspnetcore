// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Provides programmatic configuration of Kestrel-specific features.
    /// </summary>
    public class KestrelServerOptions
    {
        /// <summary>
        /// Configures the endpoints that Kestrel should listen to.
        /// </summary>
        /// <remarks>
        /// If this list is empty, the server.urls setting (e.g. UseUrls) is used.
        /// </remarks>
        internal List<ListenOptions> ListenOptions { get; } = new List<ListenOptions>();

        /// <summary>
        /// Gets or sets whether the <c>Server</c> header should be included in each response.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool AddServerHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool AllowSynchronousIO { get; set; } = false;

        /// <summary>
        /// Gets or sets a value that controls whether the string values materialized
        /// will be reused across requests; if they match, or if the strings will always be reallocated.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool DisableStringReuse { get; set; } = false;

        /// <summary>
        /// Enables the Listen options callback to resolve and use services registered by the application during startup.
        /// Typically initialized by UseKestrel()"/>.
        /// </summary>
        public IServiceProvider ApplicationServices { get; set; }

        /// <summary>
        /// Provides access to request limit options.
        /// </summary>
        public KestrelServerLimits Limits { get; } = new KestrelServerLimits();

        /// <summary>
        /// Provides a configuration source where endpoints will be loaded from on server start.
        /// The default is null.
        /// </summary>
        public KestrelConfigurationLoader ConfigurationLoader { get; set; }

        /// <summary>
        /// Controls whether to return the AltSvcHeader from on an HTTP/2 or lower response for HTTP/3
        /// </summary>
        public bool EnableAltSvc { get; set; } = false;

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
        internal X509Certificate2 DefaultCertificate { get; set; }

        /// <summary>
        /// Has the default dev certificate load been attempted?
        /// </summary>
        internal bool IsDevCertLoaded { get; set; }

        /// <summary>
        /// Treat request headers as Latin-1 or ISO/IEC 8859-1 instead of UTF-8.
        /// </summary>
        internal bool Latin1RequestHeaders { get; set; }

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
            ConfigurationLoader?.ApplyConfigurationDefaults(listenOptions);
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

        private void EnsureDefaultCert()
        {
            if (DefaultCertificate == null && !IsDevCertLoaded)
            {
                IsDevCertLoaded = true; // Only try once
                var logger = ApplicationServices.GetRequiredService<ILogger<KestrelServer>>();
                try
                {
                    DefaultCertificate = CertificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true)
                        .FirstOrDefault();

                    if (DefaultCertificate != null)
                    {
                        logger.LocatedDevelopmentCertificate(DefaultCertificate);
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
        public KestrelConfigurationLoader Configure()
        {
            var loader = new KestrelConfigurationLoader(this, new ConfigurationBuilder().Build());
            ConfigurationLoader = loader;
            return loader;
        }

        /// <summary>
        /// Creates a configuration loader for setting up Kestrel that takes an IConfiguration as input.
        /// This configuration must be scoped to the configuration section for Kestrel.
        /// </summary>
        public KestrelConfigurationLoader Configure(IConfiguration config)
        {
            var loader = new KestrelConfigurationLoader(this, config);
            ConfigurationLoader = loader;
            return loader;
        }

        /// <summary>
        /// Bind to given IP address and port.
        /// </summary>
        public void Listen(IPAddress address, int port)
        {
            Listen(address, port, _ => { });
        }

        /// <summary>
        /// Bind to given IP address and port.
        /// The callback configures endpoint-specific settings.
        /// </summary>
        public void Listen(IPAddress address, int port, Action<ListenOptions> configure)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Listen(new IPEndPoint(address, port), configure);
        }

        /// <summary>
        /// Bind to given IP endpoint.
        /// </summary>
        public void Listen(IPEndPoint endPoint)
        {
            Listen(endPoint, _ => { });
        }

        /// <summary>
        /// Bind to given IP address and port.
        /// The callback configures endpoint-specific settings.
        /// </summary>
        public void Listen(IPEndPoint endPoint, Action<ListenOptions> configure)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(endPoint);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new LocalhostListenOptions(port);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new AnyIPListenOptions(port);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
        }

        /// <summary>
        /// Bind to given Unix domain socket path.
        /// </summary>
        public void ListenUnixSocket(string socketPath)
        {
            ListenUnixSocket(socketPath, _ => { });
        }

        /// <summary>
        /// Bind to given Unix domain socket path.
        /// Specify callback to configure endpoint-specific settings.
        /// </summary>
        public void ListenUnixSocket(string socketPath, Action<ListenOptions> configure)
        {
            if (socketPath == null)
            {
                throw new ArgumentNullException(nameof(socketPath));
            }

            if (!Path.IsPathRooted(socketPath))
            {
                throw new ArgumentException(CoreStrings.UnixSocketPathMustBeAbsolute, nameof(socketPath));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(socketPath);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(handle);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
        }
    }
}
