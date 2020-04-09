// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class HttpsConnectionMiddlewareTests : LoggedTest
    {
        private static X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();
        private static X509Certificate2 _x509Certificate2NoExt = TestResources.GetTestCertificate("no_extensions.pfx");

        [Fact]
        public async Task CanReadAndWriteWithHttpsConnectionMiddleware()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
            };

            await using (var server = new TestServer(App, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                var result = await server.HttpClientSlim.PostAsync($"https://localhost:{server.Port}/",
                    new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("content", "Hello World?")
                    }),
                    validateCertificate: false);

                Assert.Equal("content=Hello+World%3F", result);
            }
        }

        [Fact]
        public async Task HandshakeDetailsAreAvailable()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
            };

            await using (var server = new TestServer(context =>
            {
                var tlsFeature = context.Features.Get<ITlsHandshakeFeature>();
                Assert.NotNull(tlsFeature);
                Assert.True(tlsFeature.Protocol > SslProtocols.None, "Protocol");
                Assert.True(tlsFeature.CipherAlgorithm > CipherAlgorithmType.Null, "Cipher");
                Assert.True(tlsFeature.CipherStrength > 0, "CipherStrength");
                Assert.True(tlsFeature.HashAlgorithm >= HashAlgorithmType.None, "HashAlgorithm"); // May be None on Linux.
                Assert.True(tlsFeature.HashStrength >= 0, "HashStrength"); // May be 0 for some algorithms
                Assert.True(tlsFeature.KeyExchangeAlgorithm > ExchangeAlgorithmType.None, "KeyExchangeAlgorithm");
                Assert.True(tlsFeature.KeyExchangeStrength >= 0, "KeyExchangeStrength"); // May be 0 on mac

                return context.Response.WriteAsync("hello world");
            }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("hello world", result);
            }
        }

        [Fact]
        public async Task RequireCertificateFailsWhenNoCertificate()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = ClientCertificateMode.RequireCertificate
            });

            await using (var server = new TestServer(App, new TestServiceContext(LoggerFactory), listenOptions))
            {
                await Assert.ThrowsAnyAsync<Exception>(
                    () => server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/"));
            }
        }

        [Fact]
        public async Task AllowCertificateContinuesWhenNoCertificate()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = ClientCertificateMode.AllowCertificate
                });
            }

            await using (var server = new TestServer(context =>
                {
                    var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.Null(tlsFeature.ClientCertificate);
                    return context.Response.WriteAsync("hello world");
                }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("hello world", result);
            }
        }

        [Fact]
        public void ThrowsWhenNoServerCertificateIsProvided()
        {
            Assert.Throws<ArgumentException>(() => new HttpsConnectionMiddleware(context => Task.CompletedTask,
                new HttpsConnectionAdapterOptions())
                );
        }

        [Fact]
        public async Task UsesProvidedServerCertificate()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
            };

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                }
            }
        }

        [Fact]
        public async Task UsesProvidedServerCertificateSelector()
        {
            var selectorCalled = 0;
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (connection, name) =>
                    {
                        Assert.NotNull(connection);
                        Assert.NotNull(connection.Features.Get<SslStream>());
                        Assert.Equal("localhost", name);
                        selectorCalled++;
                        return _x509Certificate2;
                    }
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                    Assert.Equal(1, selectorCalled);
                }
            }
        }

        [Fact]
        public async Task UsesProvidedServerCertificateSelectorEachTime()
        {
            var selectorCalled = 0;
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (connection, name) =>
                    {
                        Assert.NotNull(connection);
                        Assert.NotNull(connection.Features.Get<SslStream>());
                        Assert.Equal("localhost", name);
                        selectorCalled++;
                        if (selectorCalled == 1)
                        {
                            return _x509Certificate2;
                        }
                        return _x509Certificate2NoExt;
                    }
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                    Assert.Equal(1, selectorCalled);
                }
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2NoExt));
                    Assert.Equal(2, selectorCalled);
                }
            }
        }

        [Fact]
        public async Task UsesProvidedServerCertificateSelectorValidatesEkus()
        {
            var selectorCalled = 0;
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (features, name) =>
                    {
                        selectorCalled++;
                        return TestResources.GetTestCertificate("eku.code_signing.pfx");
                    }
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await Assert.ThrowsAsync<IOException>(() =>
                        stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false));
                    Assert.Equal(1, selectorCalled);
                }
            }
        }

        [Fact]
        public async Task UsesProvidedServerCertificateSelectorOverridesServerCertificate()
        {
            var selectorCalled = 0;
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = _x509Certificate2NoExt,
                    ServerCertificateSelector = (connection, name) =>
                    {
                        Assert.NotNull(connection);
                        Assert.NotNull(connection.Features.Get<SslStream>());
                        Assert.Equal("localhost", name);
                        selectorCalled++;
                        return _x509Certificate2;
                    }
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                    Assert.Equal(1, selectorCalled);
                }
            }
        }

        [Fact]
        public async Task UsesProvidedServerCertificateSelectorFailsIfYouReturnNull()
        {
            var selectorCalled = 0;
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (features, name) =>
                    {
                        selectorCalled++;
                        return null;
                    }
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStream(connection.Stream);
                    await Assert.ThrowsAsync<IOException>(() =>
                        stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false));
                    Assert.Equal(1, selectorCalled);
                }
            }
        }

        [Theory]
        [InlineData(HttpProtocols.Http1)]
        [InlineData(HttpProtocols.Http1AndHttp2)] // Make sure Http/1.1 doesn't regress with Http/2 enabled.
        public async Task CertificatePassedToHttpContext(HttpProtocols httpProtocols)
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.Protocols = httpProtocols;
                listenOptions.UseHttps(options =>
                {
                    options.ServerCertificate = _x509Certificate2;
                    options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    options.AllowAnyClientCertificate();
                });
            }

            await using (var server = new TestServer(context =>
                {
                    var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.NotNull(tlsFeature.ClientCertificate);
                    Assert.NotNull(context.Connection.ClientCertificate);
                    return context.Response.WriteAsync("hello world");
                }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // SslStream is used to ensure the certificate is actually passed to the server
                    // HttpClient might not send the certificate because it is invalid or it doesn't match any
                    // of the certificate authorities sent by the server in the SSL handshake.
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, true);
                }
            }
        }

        [Fact]
        public async Task HttpsSchemePassedToRequestFeature()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
            }

            await using (var server = new TestServer(context => context.Response.WriteAsync(context.Request.Scheme), new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("https", result);
            }
        }

        [Fact]
        public async Task DoesNotSupportTls10()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(options =>
                {
                    options.ServerCertificate = _x509Certificate2;
                    options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    options.AllowAnyClientCertificate();
                });
            }


            await using (var server = new TestServer(context => context.Response.WriteAsync("hello world"), new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                // SslStream is used to ensure the certificate is actually passed to the server
                // HttpClient might not send the certificate because it is invalid or it doesn't match any
                // of the certificate authorities sent by the server in the SSL handshake.
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    var ex = await Assert.ThrowsAnyAsync<Exception>(
                        async () => await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls, false));
                }
            }
        }

        [Theory]
        [InlineData(ClientCertificateMode.AllowCertificate)]
        [InlineData(ClientCertificateMode.RequireCertificate)]
        public async Task ClientCertificateValidationGetsCalledWithNotNullParameters(ClientCertificateMode mode)
        {
            var clientCertificateValidationCalled = false;
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = mode,
                    ClientCertificateValidation = (certificate, chain, sslPolicyErrors) =>
                    {
                        clientCertificateValidationCalled = true;
                        Assert.NotNull(certificate);
                        Assert.NotNull(chain);
                        return true;
                    }
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, true);
                    Assert.True(clientCertificateValidationCalled);
                }
            }
        }

        [ConditionalTheory]
        [InlineData(ClientCertificateMode.AllowCertificate)]
        [InlineData(ClientCertificateMode.RequireCertificate)]
        public async Task ValidationFailureRejectsConnection(ClientCertificateMode mode)
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = mode,
                    ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => false
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, false);
                }
            }
        }

        [Theory]
        [InlineData(ClientCertificateMode.AllowCertificate)]
        [InlineData(ClientCertificateMode.RequireCertificate)]
        public async Task RejectsConnectionOnSslPolicyErrorsWhenNoValidation(ClientCertificateMode mode)
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = mode
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, false);
                }
            }
        }

        [Fact]
        public async Task AllowAnyCertOverridesCertificateValidation()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(options =>
                {
                    options.ServerCertificate = _x509Certificate2;
                    options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    options.ClientCertificateValidation = (certificate, x509Chain, sslPolicyErrors) => false;
                    options.AllowAnyClientCertificate();
                });
            }

            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, true);
                }
            }
        }

        [Fact]
        public async Task CertificatePassedToHttpContextIsNotDisposed()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(options =>
                {
                    options.ServerCertificate = _x509Certificate2;
                    options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    options.AllowAnyClientCertificate();
                });
            }

            RequestDelegate app = context =>
            {
                var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tlsFeature);
                Assert.NotNull(tlsFeature.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate.PublicKey);
                return context.Response.WriteAsync("hello world");
            };

            await using (var server = new TestServer(app, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var stream = OpenSslStreamWithCert(connection.Stream);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, true);
                }
            }
        }

        [Theory]
        [InlineData("no_extensions.pfx")]
        public void AcceptsCertificateWithoutExtensions(string testCertName)
        {
            var certPath = TestResources.GetCertPath(testCertName);
            TestOutputHelper.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.Empty(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());

            new HttpsConnectionMiddleware(context => Task.CompletedTask, new HttpsConnectionAdapterOptions
            {
                ServerCertificate = cert,
            });
        }

        [Theory]
        [InlineData("eku.server.pfx")]
        [InlineData("eku.multiple_usages.pfx")]
        public void ValidatesEnhancedKeyUsageOnCertificate(string testCertName)
        {
            var certPath = TestResources.GetCertPath(testCertName);
            TestOutputHelper.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.NotEmpty(cert.Extensions);
            var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
            Assert.NotEmpty(eku.EnhancedKeyUsages);

            new HttpsConnectionMiddleware(context => Task.CompletedTask, new HttpsConnectionAdapterOptions
            {
                ServerCertificate = cert,
            });
        }

        [Theory]
        [InlineData("eku.code_signing.pfx")]
        [InlineData("eku.client.pfx")]
        public void ThrowsForCertificatesMissingServerEku(string testCertName)
        {
            var certPath = TestResources.GetCertPath(testCertName);
            TestOutputHelper.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.NotEmpty(cert.Extensions);
            var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
            Assert.NotEmpty(eku.EnhancedKeyUsages);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new HttpsConnectionMiddleware(context => Task.CompletedTask, new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = cert,
                }));

            Assert.Equal(CoreStrings.FormatInvalidServerCertificateEku(cert.Thumbprint), ex.Message);
        }

        [ConditionalTheory]
        [InlineData(HttpProtocols.Http1)]
        [InlineData(HttpProtocols.Http2)]
        [InlineData(HttpProtocols.Http1AndHttp2)]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/10428", Queues = "Debian.8.Amd64;Debian.8.Amd64.Open")] // Debian 8 uses OpenSSL 1.0.1 which does not support HTTP/2
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
        public async Task ListenOptionsProtolsCanBeSetAfterUseHttps(HttpProtocols httpProtocols)
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(_x509Certificate2);
                listenOptions.Protocols = httpProtocols;
            }

            await using var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions);
            using var connection = server.CreateConnection();

            var sslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11,
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 },
            };

            using var stream = OpenSslStream(connection.Stream);
            await stream.AuthenticateAsClientAsync(sslOptions);

            Assert.Equal(
                httpProtocols.HasFlag(HttpProtocols.Http2) ?
                    SslApplicationProtocol.Http2 :
                    SslApplicationProtocol.Http11,
                stream.NegotiatedApplicationProtocol);
        }

        private static async Task App(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            while (true)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
            }
        }

        private static SslStream OpenSslStream(Stream rawStream)
        {
            return new SslStream(rawStream, false, (sender, certificate, chain, errors) => true);
        }

        /// <summary>
        /// SslStream is used to ensure the certificate is actually passed to the server
        /// HttpClient might not send the certificate because it is invalid or it doesn't match any
        /// of the certificate authorities sent by the server in the SSL handshake.
        /// </summary>
        private static SslStream OpenSslStreamWithCert(Stream rawStream, X509Certificate2 clientCertificate = null)
        {
            return new SslStream(rawStream, false, (sender, certificate, chain, errors) => true,
                (sender, host, certificates, certificate, issuers) => clientCertificate ?? _x509Certificate2);
        }

        private static async Task AssertConnectionResult(SslStream stream, bool success)
        {
            var request = Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\n\r\n");
            await stream.WriteAsync(request, 0, request.Length);
            var reader = new StreamReader(stream);
            string line = null;
            if (success)
            {
                line = await reader.ReadLineAsync();
                Assert.Equal("HTTP/1.1 200 OK", line);
            }
            else
            {
                try
                {
                    line = await reader.ReadLineAsync();
                }
                catch (IOException) { }
                Assert.Null(line);
            }
        }
    }
}
