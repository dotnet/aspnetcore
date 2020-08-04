// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class SniOptionsSelectorTests
    {
        [Fact]
        public void PrefersExactNameOverWildcardPrefixOverFullWildcard()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    {
                        "www.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "ExactWWW"
                            }
                        }
                    },
                    {
                        "*.a.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "WildcardPrefixLong"
                            }
                        }
                    },
                    {
                        "*.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "WildcardPrefixShort"
                            }
                        }
                    },
                    {
                        "*",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "WildcardOnly"
                            }
                        }
                    }
                }
            };

            var mockCertificateConfigLoader = new MockCertificateConfigLoader();
            var pathDictionary = mockCertificateConfigLoader.CertToPathDictionary;

            var sniOptionsSelector = new SniOptionsSelector(
                mockCertificateConfigLoader,
                endpointConfig,
                fallbackOptions: new HttpsConnectionAdapterOptions(),
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var wwwSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            Assert.Equal("ExactWWW", pathDictionary[wwwSubdomainOptions.ServerCertificate]);

            var baSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "b.a.example.org");
            Assert.Equal("WildcardPrefixLong", pathDictionary[baSubdomainOptions.ServerCertificate]);

            var aSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "a.example.org");
            Assert.Equal("WildcardPrefixShort", pathDictionary[aSubdomainOptions.ServerCertificate]);

            // REVIEW: Are we OK with "example.org" matching "*" before "*.example.org"? Feels annoying to me.
            // The alternative would have "a.example.org" match "*.a.example.org" before "*.example.org", but that also feels bad.
            var noSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "example.org");
            Assert.Equal("WildcardOnly", pathDictionary[noSubdomainOptions.ServerCertificate]);

            var anotherTldOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "dot.net");
            Assert.Equal("WildcardOnly", pathDictionary[anotherTldOptions.ServerCertificate]);
        }


        private class MockCertificateConfigLoader : ICertificateConfigLoader
        {
            public Dictionary<object, string> CertToPathDictionary { get; } = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

            public X509Certificate2 LoadCertificate(CertificateConfig certInfo, string endpointName)
            {
                var cert = new X509Certificate2();
                CertToPathDictionary.Add(cert, certInfo.Path);
                return cert;
            }
        }

        private class MockConnectionContext : ConnectionContext
        {
            public override IDuplexPipe Transport { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            public override string ConnectionId { get; set; } = "MockConnectionId";
            public override IFeatureCollection Features { get; } = new FeatureCollection();
            public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        }
    }
}
