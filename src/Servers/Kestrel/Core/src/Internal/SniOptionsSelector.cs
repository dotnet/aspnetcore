// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                var sslOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = configLoader.LoadCertificate(sniConfig.Certificate, $"{endpointConfig.Name}:SNI:{name}"),
                    EnabledSslProtocols = sniConfig.SslProtocols ?? fallbackOptions.SslProtocols,
                    CertificateRevocationCheckMode = fallbackOptions.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                };

                if (sslOptions.ServerCertificate is null)
                {
                    if (fallbackOptions.ServerCertificate is null && _fallbackServerCertificateSelector is null)
                    {
                        throw new InvalidOperationException(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound);
                    }

                    if (_fallbackServerCertificateSelector is null)
                    {
                        // Cache the fallback ServerCertificate since there's no fallback ServerCertificateSelector taking precedence. 
                        sslOptions.ServerCertificate = fallbackOptions.ServerCertificate;
                    }
                }

                var clientCertificateMode = sniConfig.ClientCertificateMode ?? fallbackOptions.ClientCertificateMode;

                if (clientCertificateMode != ClientCertificateMode.NoCertificate)
                {
                    sslOptions.ClientCertificateRequired = true;
                    sslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                        HttpsConnectionMiddleware.RemoteCertificateValidationCallback(
                            clientCertificateMode, fallbackOptions.ClientCertificateValidation, certificate, chain, sslPolicyErrors);
                }

                var httpProtocols = sniConfig.Protocols ?? fallbackHttpProtocols;
                httpProtocols = HttpsConnectionMiddleware.ValidateAndNormalizeHttpProtocols(httpProtocols, logger);
                HttpsConnectionMiddleware.ConfigureAlpn(sslOptions, httpProtocols);

                var sniOptions = new SniOptions
                {
                    SslOptions = sslOptions,
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

        public SslServerAuthenticationOptions GetOptions(ConnectionContext connection, string serverName)
        {
            SniOptions sniOptions = null;

            if (!string.IsNullOrEmpty(serverName) && !_fullNameOptions.TryGetValue(serverName, out sniOptions))
            {
                TryGetWildcardPrefixedOptions(serverName, out sniOptions);
            }

            // Fully wildcarded ("*") options can be used even when given an empty server name.
            sniOptions ??= _wildcardHostOptions;

            if (sniOptions is null)
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

            connection.Features.Set(new HttpProtocolsFeature(sniOptions.HttpProtocols));

            var sslOptions = sniOptions.SslOptions;

            if (sslOptions.ServerCertificate is null)
            {
                Debug.Assert(_fallbackServerCertificateSelector != null,
                    "The cached SniOptions ServerCertificate can only be null if there's a fallback certificate selector.");

                // If a ServerCertificateSelector doesn't return a cert, HttpsConnectionMiddleware doesn't fallback to the ServerCertificate.
                sslOptions = CloneSslOptions(sslOptions);
                sslOptions.ServerCertificate = _fallbackServerCertificateSelector(connection, serverName);
            }

            if (_onAuthenticateCallback != null)
            {
                // From doc comments: "This is called after all of the other settings have already been applied."
                sslOptions = CloneSslOptions(sslOptions);
                _onAuthenticateCallback(connection, sslOptions);
            }

            return sslOptions;
        }

        private bool TryGetWildcardPrefixedOptions(string serverName, out SniOptions sniOptions)
        {
            sniOptions = null;

            var matchedNameLength = 0;
            ReadOnlySpan<char> serverNameSpan = serverName;

            foreach (var (nameCandidate, optionsCandidate) in _wildcardPrefixOptions)
            {
                ReadOnlySpan<char> nameCandidateSpan = nameCandidate;

                // Only slice off 1 character, the `*`. We want to match the leading `.` also.
                if (serverNameSpan.EndsWith(nameCandidateSpan.Slice(1), StringComparison.OrdinalIgnoreCase) &&
                    nameCandidateSpan.Length > matchedNameLength)
                {
                    matchedNameLength = nameCandidateSpan.Length;
                    sniOptions = optionsCandidate;
                }
            }

            return sniOptions != null;
        }

        // TODO: Reflection based test to ensure we clone everything!
        // This won't catch issues related to mutable subproperties, but the existing subproperties look like they're mosly immutable.
        // The exception are the ApplicationProtocols list which we clone and the ServerCertificate because of methods like Import() and Reset() :(
        internal static SslServerAuthenticationOptions CloneSslOptions(SslServerAuthenticationOptions sslOptions) =>
            new SslServerAuthenticationOptions
            {
                AllowRenegotiation = sslOptions.AllowRenegotiation,
                ApplicationProtocols = sslOptions.ApplicationProtocols?.ToList(),
                CertificateRevocationCheckMode = sslOptions.CertificateRevocationCheckMode,
                CipherSuitesPolicy = sslOptions.CipherSuitesPolicy,
                ClientCertificateRequired = sslOptions.ClientCertificateRequired,
                EnabledSslProtocols = sslOptions.EnabledSslProtocols,
                EncryptionPolicy = sslOptions.EncryptionPolicy,
                RemoteCertificateValidationCallback = sslOptions.RemoteCertificateValidationCallback,
                ServerCertificate = sslOptions.ServerCertificate,
                ServerCertificateContext = sslOptions.ServerCertificateContext,
                ServerCertificateSelectionCallback = sslOptions.ServerCertificateSelectionCallback,
            };

        private class SniOptions
        {
            public SslServerAuthenticationOptions SslOptions { get; set; }
            public HttpProtocols HttpProtocols { get; set; }
        }
    }
}
