// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
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
        private readonly Dictionary<string, SslServerAuthenticationOptions> _fullNameOptions = new Dictionary<string, SslServerAuthenticationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string, SslServerAuthenticationOptions)> _wildcardPrefixOptions = new List<(string, SslServerAuthenticationOptions)>();
        private readonly SslServerAuthenticationOptions _wildcardHostOptions = null;

        public SniOptionsSelector(
            KestrelConfigurationLoader configLoader,
            EndpointConfig endpointConfig,
            HttpsConnectionAdapterOptions fallbackOptions,
            HttpProtocols fallbackHttpProtocols,
            ILogger logger)
        {
            _endpointName = endpointConfig.Name;

            foreach (var (name, sniConfig) in endpointConfig.SNI)
            {
                var sslServerOptions = new SslServerAuthenticationOptions();

                sslServerOptions.ServerCertificate = configLoader.LoadCertificate(sniConfig.Certificate, endpointConfig.Name)
                    ?? configLoader.LoadEndpointOrDefaultCertificate(fallbackOptions, endpointConfig);

                if (sslServerOptions.ServerCertificate is null)
                {
                    throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
                }

                sslServerOptions.EnabledSslProtocols = sniConfig.SslProtocols ?? fallbackOptions.SslProtocols;

                var httpProtocols = sniConfig.Protocols ?? fallbackHttpProtocols;
                httpProtocols = HttpsConnectionMiddleware.ValidateAndNormalizeHttpProtocols(httpProtocols, logger);
                HttpsConnectionMiddleware.ConfigureAlpn(sslServerOptions, httpProtocols);

                var clientCertificateMode = sniConfig.ClientCertificateMode ?? fallbackOptions.ClientCertificateMode;

                if (clientCertificateMode != ClientCertificateMode.NoCertificate)
                {
                    sslServerOptions.ClientCertificateRequired = true;
                    sslServerOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                        HttpsConnectionMiddleware.RemoteCertificateValidationCallback(
                            clientCertificateMode, fallbackOptions.ClientCertificateValidation, certificate, chain, sslPolicyErrors);
                }

                if (name.Equals(wildcardHost, StringComparison.Ordinal))
                {
                    _wildcardHostOptions = sslServerOptions;
                }
                else if (name.StartsWith(wildcardPrefix, StringComparison.Ordinal))
                {
                    _wildcardPrefixOptions.Add((name, sslServerOptions));
                }
                else
                {
                    _fullNameOptions[name] = sslServerOptions;
                }
            }
        }

        public SslServerAuthenticationOptions GetOptions(string serverName)
        {
            SslServerAuthenticationOptions options = null;

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
                    if (serverNameSpan.EndsWith(nameCandidateSpan.Slice(wildcardHost.Length), StringComparison.OrdinalIgnoreCase)
                        && nameCandidateSpan.Length > matchedNameLength)
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

            return options;
        }
    }
}
