// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class ConfigurationReader
    {
        private const string ProtocolsKey = "Protocols";
        private const string CertificatesKey = "Certificates";
        private const string CertificateKey = "Certificate";
        private const string EndpointDefaultsKey = "EndpointDefaults";
        private const string EndpointsKey = "Endpoints";
        private const string UrlKey = "Url";
        private const string Latin1RequestHeadersKey = "Latin1RequestHeaders";

        private readonly IConfiguration _configuration;

        public ConfigurationReader(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Certificates = ReadCertificates();
            EndpointDefaults = ReadEndpointDefaults();
            Endpoints = ReadEndpoints();
            Latin1RequestHeaders = _configuration.GetValue<bool>(Latin1RequestHeadersKey);
        }

        public IDictionary<string, CertificateConfig> Certificates { get; }
        public EndpointDefaults EndpointDefaults { get; }
        public IEnumerable<EndpointConfig> Endpoints { get; }
        public bool Latin1RequestHeaders  { get; }

        private IDictionary<string, CertificateConfig> ReadCertificates()
        {
            var certificates = new Dictionary<string, CertificateConfig>(0);

            var certificatesConfig = _configuration.GetSection(CertificatesKey).GetChildren();
            foreach (var certificateConfig in certificatesConfig)
            {
                certificates.Add(certificateConfig.Key, new CertificateConfig(certificateConfig));
            }

            return certificates;
        }

        // "EndpointDefaults": {
        //    "Protocols": "Http1AndHttp2",
        // }
        private EndpointDefaults ReadEndpointDefaults()
        {
            var configSection = _configuration.GetSection(EndpointDefaultsKey);
            return new EndpointDefaults
            {
                Protocols = ParseProtocols(configSection[ProtocolsKey])
            };
        }

        private IEnumerable<EndpointConfig> ReadEndpoints()
        {
            var endpoints = new List<EndpointConfig>();

            var endpointsConfig = _configuration.GetSection(EndpointsKey).GetChildren();
            foreach (var endpointConfig in endpointsConfig)
            {
                // "EndpointName": {
                //    "Url": "https://*:5463",
                //    "Protocols": "Http1AndHttp2",
                //    "Certificate": {
                //        "Path": "testCert.pfx",
                //        "Password": "testPassword"
                //    }
                // }

                var url = endpointConfig[UrlKey];
                if (string.IsNullOrEmpty(url))
                {
                    throw new InvalidOperationException(CoreStrings.FormatEndpointMissingUrl(endpointConfig.Key));
                }

                var endpoint = new EndpointConfig
                {
                    Name = endpointConfig.Key,
                    Url = url,
                    Protocols = ParseProtocols(endpointConfig[ProtocolsKey]),
                    ConfigSection = endpointConfig,
                    Certificate = new CertificateConfig(endpointConfig.GetSection(CertificateKey)),
                };

                endpoints.Add(endpoint);
            }

            return endpoints;
        }

        private static HttpProtocols? ParseProtocols(string protocols)
        {
            if (Enum.TryParse<HttpProtocols>(protocols, ignoreCase: true, out var result))
            {
                return result;
            }

            return null;
        }
    }

    // "EndpointDefaults": {
    //    "Protocols": "Http1AndHttp2",
    // }
    internal class EndpointDefaults
    {
        public HttpProtocols? Protocols { get; set; }
    }

    // "EndpointName": {
    //    "Url": "https://*:5463",
    //    "Protocols": "Http1AndHttp2",
    //    "Certificate": {
    //        "Path": "testCert.pfx",
    //        "Password": "testPassword"
    //    }
    // }
    internal class EndpointConfig
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public HttpProtocols? Protocols { get; set; }

        // REVIEW: ConfigSection doesn't seem comparable. If someone changes a custom key and consumes it in
        // their Action<EndpointConfiguration>, we won't rebind.
        public IConfigurationSection ConfigSection { get; set; }
        public CertificateConfig Certificate { get; set; }

        public override bool Equals(object obj) =>
            obj is EndpointConfig other &&
            Name == other.Name &&
            Url == other.Url &&
            (Protocols ?? ListenOptions.DefaultHttpProtocols) == (other.Protocols ?? ListenOptions.DefaultHttpProtocols) &&
            Certificate == other.Certificate;

        public override int GetHashCode() => HashCode.Combine(Name, Url, Protocols ?? ListenOptions.DefaultHttpProtocols, Certificate);

        public static bool operator ==(EndpointConfig lhs, EndpointConfig rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);
        public static bool operator !=(EndpointConfig lhs, EndpointConfig rhs) => !(lhs == rhs);
    }

    // "CertificateName": {
    //      "Path": "testCert.pfx",
    //      "Password": "testPassword"
    // }
    internal class CertificateConfig
    {
        public CertificateConfig(IConfigurationSection configSection)
        {
            ConfigSection = configSection;
            ConfigSection.Bind(this);
        }

        public IConfigurationSection ConfigSection { get; }

        // File
        public bool IsFileCert => !string.IsNullOrEmpty(Path);

        public string Path { get; set; }

        public string Password { get; set; }

        // Cert store

        public bool IsStoreCert => !string.IsNullOrEmpty(Subject);

        public string Subject { get; set; }

        public string Store { get; set; }

        public string Location { get; set; }

        public bool? AllowInvalid { get; set; }

        public override bool Equals(object obj) =>
            obj is CertificateConfig other &&
            Path == other.Path &&
            Password == other.Password &&
            Subject == other.Subject &&
            Store == other.Store &&
            Location == other.Location &&
            (AllowInvalid ?? false) == (other.AllowInvalid ?? false);

        public override int GetHashCode() => HashCode.Combine(Path, Password, Subject, Store, Location, AllowInvalid ?? false);

        public static bool operator ==(CertificateConfig lhs, CertificateConfig rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);
        public static bool operator !=(CertificateConfig lhs, CertificateConfig rhs) => !(lhs == rhs);
    }
}
