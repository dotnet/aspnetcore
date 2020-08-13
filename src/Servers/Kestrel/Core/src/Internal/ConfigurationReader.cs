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

        private IConfiguration _configuration;
        private IDictionary<string, CertificateConfig> _certificates;
        private IList<EndpointConfig> _endpoints;
        private EndpointDefaults _endpointDefaults;
        private bool? _latin1RequestHeaders;

        public ConfigurationReader(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IDictionary<string, CertificateConfig> Certificates
        {
            get
            {
                if (_certificates == null)
                {
                    ReadCertificates();
                }

                return _certificates;
            }
        }

        public EndpointDefaults EndpointDefaults
        {
            get
            {
                if (_endpointDefaults == null)
                {
                    ReadEndpointDefaults();
                }

                return _endpointDefaults;
            }
        }

        public IEnumerable<EndpointConfig> Endpoints
        {
            get
            {
                if (_endpoints == null)
                {
                    ReadEndpoints();
                }

                return _endpoints;
            }
        }

        public bool Latin1RequestHeaders
        {
            get
            {
                if (_latin1RequestHeaders is null)
                {
                    _latin1RequestHeaders = _configuration.GetValue<bool>(Latin1RequestHeadersKey);
                }

                return _latin1RequestHeaders.Value;
            }
        }

        private void ReadCertificates()
        {
            _certificates = new Dictionary<string, CertificateConfig>(0);

            var certificatesConfig = _configuration.GetSection(CertificatesKey).GetChildren();
            foreach (var certificateConfig in certificatesConfig)
            {
                _certificates.Add(certificateConfig.Key, new CertificateConfig(certificateConfig));
            }
        }

        // "EndpointDefaults": {
        //    "Protocols": "Http1AndHttp2",
        // }
        private void ReadEndpointDefaults()
        {
            var configSection = _configuration.GetSection(EndpointDefaultsKey);
            _endpointDefaults = new EndpointDefaults
            {
                Protocols = ParseProtocols(configSection[ProtocolsKey])
            };
        }

        private void ReadEndpoints()
        {
            _endpoints = new List<EndpointConfig>();

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
                _endpoints.Add(endpoint);
            }
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
        public IConfigurationSection ConfigSection { get; set; }
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
        public IConfigurationSection ConfigSection { get; set; }
        public CertificateConfig Certificate { get; set; }
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
    }
}
