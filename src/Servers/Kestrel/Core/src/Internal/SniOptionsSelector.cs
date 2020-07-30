// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class SniOptionsSelector
    {
        private const string wildcardHost = "*";
        private const string wildcardPrefix = "*.";

        private readonly string _endpointName;

        private readonly Func<ConnectionContext, string, X509Certificate2> _fallbackServerCertificateSelector;
        private readonly Action<ConnectionContext, SslServerAuthenticationOptions> _onAuthenticateCallback;

        private readonly Dictionary<string, SniOptions> _fullNameOptions = new Dictionary<string, SniOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string, SniOptions)> _wildcardPrefixOptions = new List<(string, SniOptions)>();
        private readonly SniOptions _wildcardHostOptions = null;

        public SniOptionsSelector(
            KestrelConfigurationLoader configLoader,
            EndpointConfig endpointConfig,
            HttpsConnectionAdapterOptions fallbackOptions,
            HttpProtocols fallbackHttpProtocols,
            ILogger logger)
        {
            _endpointName = endpointConfig.Name;

            _fallbackServerCertificateSelector = fallbackOptions.ServerCertificateSelector;
            _onAuthenticateCallback = fallbackOptions.OnAuthenticate;

            foreach (var (name, sniConfig) in endpointConfig.SNI)
            {
                var sslServerOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = configLoader.LoadCertificate(sniConfig.Certificate, endpointConfig.Name),
                    EnabledSslProtocols = sniConfig.SslProtocols ?? fallbackOptions.SslProtocols,
                };

                if (sslServerOptions.ServerCertificate is null)
                {
                    if (fallbackOptions.ServerCertificate is null && fallbackOptions.ServerCertificateSelector is null)
                    {
                        throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
                    }

                    if (fallbackOptions.ServerCertificateSelector is null)
                    {
                        // Cache the fallback ServerCertificate since there's no fallback ServerCertificateSelector taking precedence. 
                        sslServerOptions.ServerCertificate = fallbackOptions.ServerCertificate;
                    }
                }

                var clientCertificateMode = sniConfig.ClientCertificateMode ?? fallbackOptions.ClientCertificateMode;

                if (clientCertificateMode != ClientCertificateMode.NoCertificate)
                {
                    sslServerOptions.ClientCertificateRequired = true;
                    sslServerOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                        HttpsConnectionMiddleware.RemoteCertificateValidationCallback(
                            clientCertificateMode, fallbackOptions.ClientCertificateValidation, certificate, chain, sslPolicyErrors);
                }

                var httpProtocols = sniConfig.Protocols ?? fallbackHttpProtocols;
                httpProtocols = HttpsConnectionMiddleware.ValidateAndNormalizeHttpProtocols(httpProtocols, logger);
                HttpsConnectionMiddleware.ConfigureAlpn(sslServerOptions, httpProtocols);

                var sniOptions = new SniOptions
                {
                    SslOptions = sslServerOptions,
                    HttpProtocols = httpProtocols,
                };

                if (name.Equals(wildcardHost, StringComparison.Ordinal))
                {
                    _wildcardHostOptions = sniOptions;
                }
                else if (name.StartsWith(wildcardPrefix, StringComparison.Ordinal))
                {
                    _wildcardPrefixOptions.Add((name, sniOptions));
                }
                else
                {
                    _fullNameOptions[name] = sniOptions;
                }
            }
        }

        public SniOptions GetOptions(ConnectionContext connection, string serverName)
        {
            SniOptions options = null;

            if (!string.IsNullOrEmpty(serverName))
            {
                if (_fullNameOptions.TryGetValue(serverName, out options))
                {
                    return options;
                }

                var matchedNameLength = 0;
                ReadOnlySpan<char> serverNameSpan = serverName;

                foreach (var (nameCandidate, optionsCandidate) in _wildcardPrefixOptions)
                {
                    ReadOnlySpan<char> nameCandidateSpan = nameCandidate;

                    // Note that we only slice off the `*`. We want to match the leading `.` also.
                    if (serverNameSpan.EndsWith(nameCandidateSpan.Slice(wildcardHost.Length), StringComparison.OrdinalIgnoreCase) &&
                        nameCandidateSpan.Length > matchedNameLength)
                    {
                        matchedNameLength = nameCandidateSpan.Length;
                        options = optionsCandidate;
                    }
                }
            }

            options ??= _wildcardHostOptions;

            if (options is null)
            {
                if (serverName is null)
                {
                    throw new AuthenticationException(CoreStrings.FormatSniNotConfiguredToAllowNoServerName(_endpointName));
                }
                else
                {
                    throw new AuthenticationException(CoreStrings.FormatSniNotConfiguredForServerName(serverName, _endpointName));
                }
            }

            if (options.SslOptions.ServerCertificate is null)
            {
                Debug.Assert(_fallbackServerCertificateSelector != null,
                    "The cached SniOptions ServerCertificate can only be null if there's a fallback certificate selector.");

                // If a ServerCertificateSelector passed into HttpsConnectionMiddleware via HttpsConnectionAdapterOptions doesn't return a cert,
                // HttpsConnectionMiddleware doesn't fallback to the ServerCertificate, so we don't do that here either.
                options = options.Clone();
                options.SslOptions.ServerCertificate = _fallbackServerCertificateSelector(connection, serverName);
            }

            if (_onAuthenticateCallback != null)
            {
                options = options.Clone();

                // From doc comments: "This is called after all of the other settings have already been applied."
                _onAuthenticateCallback(connection, options.SslOptions);
            }    

            return options;
        }
    }
}
