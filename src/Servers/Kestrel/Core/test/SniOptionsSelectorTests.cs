// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Security;
using System.Security.Authentication;
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
        public void PrefersExactMatchOverWildcardPrefixOverWildcardOnly()
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
                                Path = "Exact"
                            }
                        }
                    },
                    {
                        "*.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "WildcardPrefix"
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
            Assert.Equal("Exact", pathDictionary[wwwSubdomainOptions.ServerCertificate]);

            var baSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "b.a.example.org");
            Assert.Equal("WildcardPrefix", pathDictionary[baSubdomainOptions.ServerCertificate]);

            var aSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "a.example.org");
            Assert.Equal("WildcardPrefix", pathDictionary[aSubdomainOptions.ServerCertificate]);

            // "*.example.org" is preferred over "*", but "*.example.org" doesn't match "example.org".
            // REVIEW: Are we OK with "example.org" matching "*" instead of "*.example.org"? It feels annoying to me to have to configure example.org twice.
            // Unfortunately, the alternative would have "a.example.org" match "*.a.example.org" before "*.example.org", and that just seems wrong.
            var noSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "example.org");
            Assert.Equal("WildcardOnly", pathDictionary[noSubdomainOptions.ServerCertificate]);

            var anotherTldOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "dot.net");
            Assert.Equal("WildcardOnly", pathDictionary[anotherTldOptions.ServerCertificate]);
        }

        [Fact]
        public void PerfersLongerWildcardPrefixOverShorterWildcardPrefix()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    {
                        "*.a.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "Long"
                            }
                        }
                    },
                    {
                        "*.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "Short"
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

            var baSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "b.a.example.org");
            Assert.Equal("Long", pathDictionary[baSubdomainOptions.ServerCertificate]);

            // "*.a.example.org" is preferred over "*.example.org", but "a.example.org" doesn't match "*.a.example.org".
            var aSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "a.example.org");
            Assert.Equal("Short", pathDictionary[aSubdomainOptions.ServerCertificate]);
        }

        [Fact]
        public void ServerNameMatchingIsCaseInsensitive()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    {
                        "Www.Example.Org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "Exact"
                            }
                        }
                    },
                    {
                        "*.Example.Org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig
                            {
                                Path = "WildcardPrefix"
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

            var wwwSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "wWw.eXample.oRg");
            Assert.Equal("Exact", pathDictionary[wwwSubdomainOptions.ServerCertificate]);

            var baSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "B.a.eXample.oRg");
            Assert.Equal("WildcardPrefix", pathDictionary[baSubdomainOptions.ServerCertificate]);

            var aSubdomainOptions = sniOptionsSelector.GetOptions(new MockConnectionContext(), "A.eXample.oRg");
            Assert.Equal("WildcardPrefix", pathDictionary[aSubdomainOptions.ServerCertificate]);
        }

        [Fact]
        public void GetOptionsThrowsAnAuthenticationExceptionIfThereIsNoMatchingSniSection()
        {
            var endpointConfig = new EndpointConfig
            {
                Name = "TestEndpointName",
                Sni = new Dictionary<string, SniConfig>()
            };

            var mockCertificateConfigLoader = new MockCertificateConfigLoader();
            var pathDictionary = mockCertificateConfigLoader.CertToPathDictionary;

            var sniOptionsSelector = new SniOptionsSelector(
                mockCertificateConfigLoader,
                endpointConfig,
                fallbackOptions: new HttpsConnectionAdapterOptions(),
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var authExWithServerName = Assert.Throws<AuthenticationException>(() => sniOptionsSelector.GetOptions(new MockConnectionContext(), "example.org"));
            Assert.Equal(CoreStrings.FormatSniNotConfiguredForServerName("example.org", endpointConfig.Name), authExWithServerName.Message);

            var authExWithoutServerName = Assert.Throws<AuthenticationException>(() => sniOptionsSelector.GetOptions(new MockConnectionContext(), null));
            Assert.Equal(CoreStrings.FormatSniNotConfiguredToAllowNoServerName(endpointConfig.Name), authExWithoutServerName.Message);
        }

        [Fact]
        public void WildcardOnlyMatchesNullServerNameDueToNoAlpn()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
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

            var options = sniOptionsSelector.GetOptions(new MockConnectionContext(), null);
            Assert.Equal("WildcardOnly", pathDictionary[options.ServerCertificate]);
        }

        [Fact]
        public void CachesSslServerAuthenticationOptions()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    {
                        "www.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig()
                        }
                    }
                }
            };

            var sniOptionsSelector = new SniOptionsSelector(
                new MockCertificateConfigLoader(),
                endpointConfig,
                fallbackOptions: new HttpsConnectionAdapterOptions(),
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var options1 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            var options2 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            Assert.Same(options1, options2);
        }

        [Fact]
        public void ClonesSslServerAuthenticationOptionsIfAnOnAuthenticateCallbackIsDefined()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    {
                        "www.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig()
                        }
                    }
                }
            };

            SslServerAuthenticationOptions lastSeenSslOptions = null;

            var fallbackOptions = new HttpsConnectionAdapterOptions
            {
                OnAuthenticate = (context, sslOptions) =>
                {
                    lastSeenSslOptions = sslOptions;
                }
            };

            var sniOptionsSelector = new SniOptionsSelector(
                new MockCertificateConfigLoader(),
                endpointConfig,
                fallbackOptions,
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var options1 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            Assert.Same(lastSeenSslOptions, options1);

            var options2 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            Assert.Same(lastSeenSslOptions, options2);

            Assert.NotSame(options1, options2);
        }

        [Fact]
        public void ClonesSslServerAuthenticationOptionsIfTheFallbackServerCertificateSelectorIsUsed()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    {
                        "selector.example.org",
                        new SniConfig()
                    },
                    {
                        "config.example.org",
                        new SniConfig
                        {
                            Certificate = new CertificateConfig()
                        }
                    }
                }
            };

            var selectorCertificate = new X509Certificate2();

            var fallbackOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = new X509Certificate2(),
                ServerCertificateSelector = (context, serverName) => selectorCertificate
            };

            var sniOptionsSelector = new SniOptionsSelector(
                new MockCertificateConfigLoader(),
                endpointConfig,
                fallbackOptions,
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var selectorOptions1 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "selector.example.org");
            Assert.Same(selectorCertificate, selectorOptions1.ServerCertificate);

            var selectorOptions2 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "selector.example.org");
            Assert.Same(selectorCertificate, selectorOptions2.ServerCertificate);

            // The SslServerAuthenticationOptions were cloned because the cert came from the ServerCertificateSelector fallback.
            Assert.NotSame(selectorOptions1, selectorOptions2);

            var configOptions1 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "config.example.org");
            Assert.NotSame(selectorCertificate, configOptions1.ServerCertificate);

            var configOptions2 = sniOptionsSelector.GetOptions(new MockConnectionContext(), "config.example.org");
            Assert.NotSame(selectorCertificate, configOptions2.ServerCertificate);

            // The SslServerAuthenticationOptions don't need to be cloned if a static cert is defined in config for the given server name.
            Assert.Same(configOptions1, configOptions2);
        }

        [Fact]
        public void ConstructorThrowsInvalidOperationExceptionIfNoCertificateDefiniedInConfigOrFallback()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    { "www.example.org", new SniConfig() }
                }
            };

            var ex = Assert.Throws<InvalidOperationException>(
                () => new SniOptionsSelector(
                    new MockCertificateConfigLoader(),
                    endpointConfig,
                    fallbackOptions: new HttpsConnectionAdapterOptions(),
                    fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                    logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>()));

            Assert.Equal(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound, ex.Message);
        }

        [Fact]
        public void FallsBackToHttpsConnectionAdapterCertificate()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    { "www.example.org", new SniConfig() }
                }
            };

            var fallbackOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = new X509Certificate2()
            };

            var sniOptionsSelector = new SniOptionsSelector(
                new MockCertificateConfigLoader(),
                endpointConfig,
                fallbackOptions,
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var options = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            Assert.Same(fallbackOptions.ServerCertificate, options.ServerCertificate);
        }

        [Fact]
        public void FallsBackToHttpsConnectionAdapterServerCertificateSelectorOverServerCertificate()
        {
            var endpointConfig = new EndpointConfig
            {
                Sni = new Dictionary<string, SniConfig>
                {
                    { "www.example.org", new SniConfig() }
                }
            };

            var selectorCertificate = new X509Certificate2();

            var fallbackOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = new X509Certificate2(),
                ServerCertificateSelector = (context, serverName) => selectorCertificate
            };

            var sniOptionsSelector = new SniOptionsSelector(
                new MockCertificateConfigLoader(),
                endpointConfig,
                fallbackOptions,
                fallbackHttpProtocols: HttpProtocols.Http1AndHttp2,
                logger: Mock.Of<ILogger<HttpsConnectionMiddleware>>());

            var options = sniOptionsSelector.GetOptions(new MockConnectionContext(), "www.example.org");
            Assert.Same(selectorCertificate, options.ServerCertificate);
        }

        private class MockCertificateConfigLoader : ICertificateConfigLoader
        {
            public Dictionary<object, string> CertToPathDictionary { get; } = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

            public X509Certificate2 LoadCertificate(CertificateConfig certInfo, string endpointName)
            {
                if (certInfo is null)
                {
                    return null;
                }

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
