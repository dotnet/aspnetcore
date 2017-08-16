// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class HttpsConnectionAdapterTests
    {
        private static X509Certificate2 _x509Certificate2 = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");
        private readonly ITestOutputHelper _output;

        public HttpsConnectionAdapterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [Fact]
        public async Task CanReadAndWriteWithHttpsConnectionAdapter()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 })
                }
            };

            using (var server = new TestServer(App, serviceContext, listenOptions))
            {
                var result = await HttpClientSlim.PostAsync($"https://localhost:{server.Port}/",
                    new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("content", "Hello World?")
                    }),
                    validateCertificate: false);

                Assert.Equal("content=Hello+World%3F", result);
            }
        }

        [Fact]
        public async Task RequireCertificateFailsWhenNoCertificate()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = ClientCertificateMode.RequireCertificate
                    })
                }
            };


            using (var server = new TestServer(App, serviceContext, listenOptions))
            {
                await Assert.ThrowsAnyAsync<Exception>(
                    () => HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/"));
            }
        }

        [Fact]
        public async Task AllowCertificateContinuesWhenNoCertificate()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = ClientCertificateMode.AllowCertificate
                    })
                }
            };

            using (var server = new TestServer(context =>
                {
                    var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.Null(tlsFeature.ClientCertificate);
                    return context.Response.WriteAsync("hello world");
                },
                serviceContext, listenOptions))
            {
                var result = await HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("hello world", result);
            }
        }

        [Fact]
        public void ThrowsWhenNoServerCertificateIsProvided()
        {
            Assert.Throws<ArgumentException>(() => new HttpsConnectionAdapter(
                new HttpsConnectionAdapterOptions())
                );
        }

        [Fact]
        public async Task UsesProvidedServerCertificate()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 })
                }
            };

            using (var server = new TestServer(context => Task.CompletedTask, serviceContext, listenOptions))
            {
                using (var client = new TcpClient())
                {
                    // SslStream is used to ensure the certificate is actually passed to the server
                    // HttpClient might not send the certificate because it is invalid or it doesn't match any
                    // of the certificate authorities sent by the server in the SSL handshake.
                    var stream = await OpenSslStream(client, server);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                }
            }
        }


        [Fact]
        public async Task CertificatePassedToHttpContext()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                        ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true
                    })
                }
            };

            using (var server = new TestServer(context =>
                {
                    var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.NotNull(tlsFeature.ClientCertificate);
                    Assert.NotNull(context.Connection.ClientCertificate);
                    return context.Response.WriteAsync("hello world");
                },
                serviceContext, listenOptions))
            {
                using (var client = new TcpClient())
                {
                    // SslStream is used to ensure the certificate is actually passed to the server
                    // HttpClient might not send the certificate because it is invalid or it doesn't match any
                    // of the certificate authorities sent by the server in the SSL handshake.
                    var stream = await OpenSslStream(client, server);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, true);
                }
            }
        }

        [Fact]
        public async Task HttpsSchemePassedToRequestFeature()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 })
                }
            };
            var serviceContext = new TestServiceContext();

            using (var server = new TestServer(context => context.Response.WriteAsync(context.Request.Scheme), serviceContext, listenOptions))
            {
                var result = await HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("https", result);
            }
        }

        [Fact]
        public async Task DoesNotSupportTls10()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                        ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true
                    })
                }
            };

            using (var server = new TestServer(context => context.Response.WriteAsync("hello world"), serviceContext, listenOptions))
            {
                // SslStream is used to ensure the certificate is actually passed to the server
                // HttpClient might not send the certificate because it is invalid or it doesn't match any
                // of the certificate authorities sent by the server in the SSL handshake.
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
                    var ex = await Assert.ThrowsAsync<IOException>(
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
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
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
                    })
                }
            };

            using (var server = new TestServer(context => Task.CompletedTask, serviceContext, listenOptions))
            {
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, true);
                    Assert.True(clientCertificateValidationCalled);
                }
            }
        }

        [Theory]
        [InlineData(ClientCertificateMode.AllowCertificate)]
        [InlineData(ClientCertificateMode.RequireCertificate)]
        public async Task ValidationFailureRejectsConnection(ClientCertificateMode mode)
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = mode,
                        ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => false
                    })
                }
            };

            using (var server = new TestServer(context => Task.CompletedTask, serviceContext, listenOptions))
            {
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
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
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = mode
                    })
                }
            };

            using (var server = new TestServer(context => Task.CompletedTask, serviceContext, listenOptions))
            {
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, false);
                }
            }
        }

        [Fact]
        public async Task CertificatePassedToHttpContextIsNotDisposed()
        {
            var serviceContext = new TestServiceContext();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                        ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true
                    })
                }
            };

            RequestDelegate app = context =>
            {
                var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tlsFeature);
                Assert.NotNull(tlsFeature.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate.PublicKey);
                return context.Response.WriteAsync("hello world");
            };

            using (var server = new TestServer(app, serviceContext, listenOptions))
            {
                // SslStream is used to ensure the certificate is actually passed to the server
                // HttpClient might not send the certificate because it is invalid or it doesn't match any
                // of the certificate authorities sent by the server in the SSL handshake.
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
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
            _output.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.Empty(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());

            new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
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
            _output.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.NotEmpty(cert.Extensions);
            var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
            Assert.NotEmpty(eku.EnhancedKeyUsages);

            new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
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
            _output.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.NotEmpty(cert.Extensions);
            var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
            Assert.NotEmpty(eku.EnhancedKeyUsages);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = cert,
                }));

            Assert.Equal(HttpsStrings.FormatInvalidServerCertificateEku(cert.Thumbprint), ex.Message);
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

        private static async Task<SslStream> OpenSslStream(TcpClient client, TestServer server, X509Certificate2 clientCertificate = null)
        {
            await client.ConnectAsync("127.0.0.1", server.Port);
            var stream = new SslStream(client.GetStream(), false, (sender, certificate, chain, errors) => true,
                (sender, host, certificates, certificate, issuers) => clientCertificate ?? _x509Certificate2);

            return stream;
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
