// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        // internal to fast-path header decoding when RequestHeaderEncodingSelector is unchanged.
        internal static readonly Func<string, Encoding?> DefaultRequestHeaderEncodingSelector = _ => null;
        internal static readonly Func<string, Encoding> DefaultLatin1RequestHeaderEncodingSelector = _ => Encoding.Latin1;

        private Func<string, Encoding?> _requestHeaderEncodingSelector = DefaultRequestHeaderEncodingSelector;

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
        /// https://tools.ietf.org/html/rfc7541#section-7.
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
        /// Controls whether to return the AltSvcHeader from on an HTTP/2 or lower response for HTTP/3
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool EnableAltSvc { get; set; } = false;

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
        /// Enables the Listen options callback to resolve and use services registered by the application during startup.
        /// Typically initialized by UseKestrel()"/>.
        /// </summary>
        public IServiceProvider? ApplicationServices { get; set; }

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

        internal Func<string, Encoding?> GetRequestHeaderEncodingSelector()
        {
            if (ReferenceEquals(_requestHeaderEncodingSelector, DefaultRequestHeaderEncodingSelector) && Latin1RequestHeaders)
            {
                return DefaultLatin1RequestHeaderEncodingSelector;
            }

            return _requestHeaderEncodingSelector;
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
                var logger = ApplicationServices!.GetRequiredService<ILogger<KestrelServer>>();
                try
                {
                    DefaultCertificate = CertificateManager.Instance.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true)
                        .FirstOrDefault();

                    if (DefaultCertificate != null)
                    {
                        var status = CertificateManager.Instance.CheckCertificateState(DefaultCertificate, interactive: false);
                        if (!status.Result)
                        {
                            // Display a warning indicating to the user that a prompt might appear and provide instructions on what to do in that
                            // case. The underlying implementation of this check is specific to Mac OS and is handled within CheckCertificateState.
                            // Kestrel must NEVER cause a UI prompt on a production system. We only attempt this here because Mac OS is not supported
                            // in production.
                            logger.DeveloperCertificateFirstRun(status.Message);

                            // Now that we've displayed a warning in the logs so that the user gets a notification that a prompt might appear, try
                            // and access the certificate key, which might trigger a prompt.
                            status = CertificateManager.Instance.CheckCertificateState(DefaultCertificate, interactive: true);
                            if (!status.Result)
                            {
                                logger.BadDeveloperCertificateState();
                            }
                        }

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
            var loader = new KestrelConfigurationLoader(this, config, reloadOnChange);
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
        /// Bind to given IP address and port.
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new AnyIPListenOptions(port);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            CodeBackedListenOptions.Add(listenOptions);
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(handle);
            ApplyEndpointDefaults(listenOptions);
            configure(listenOptions);
            CodeBackedListenOptions.Add(listenOptions);
        }
    }
}
