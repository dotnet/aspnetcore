// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class HttpsConnectionFilterTests
    {
        private static string _serverAddress = "https://127.0.0.1:0/";
        private static X509Certificate2 _x509Certificate2 = new X509Certificate2(@"TestResources/testCert.pfx", "testPassword");

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task CanReadAndWriteWithHttpsConnectionFilter()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                    new HttpsConnectionFilterOptions { ServerCertificate = _x509Certificate2 },
                    new NoOpConnectionFilter())
            );

            using (var server = new TestServer(App, serviceContext, _serverAddress))
            {
                var result = await HttpClientSlim.PostAsync($"https://localhost:{server.Port}/",
                    new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("content", "Hello World?")
                    }),
                    validateCertificate: false);

                Assert.Equal("content=Hello+World%3F", result);
            }
        }

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task RequireCertificateFailsWhenNoCertificate()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                    new HttpsConnectionFilterOptions
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateMode = ClientCertificateMode.RequireCertificate
                    },
                    new NoOpConnectionFilter())
            );

            using (var server = new TestServer(App, serviceContext, _serverAddress))
            {
                await Assert.ThrowsAnyAsync<Exception>(
                    () => HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/"));
            }
        }

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task AllowCertificateContinuesWhenNoCertificate()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = ClientCertificateMode.AllowCertificate
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context =>
                {
                    Assert.Equal(context.Features.Get<ITlsConnectionFeature>(), null);
                    return context.Response.WriteAsync("hello world");
                },
                serviceContext, _serverAddress))
            {
                var result = await HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("hello world", result);
            }
        }

        [Fact]
        public void ThrowsWhenNoServerCertificateIsProvided()
        {
            Assert.Throws<ArgumentException>(() => new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions(),
                new NoOpConnectionFilter())
                );
        }

        [Fact]
        public async Task UsesProvidedServerCertificate()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context => TaskCache.CompletedTask, serviceContext, _serverAddress))
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


        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task CertificatePassedToHttpContext()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                    ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context =>
                {
                    var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.NotNull(tlsFeature.ClientCertificate);
                    Assert.NotNull(context.Connection.ClientCertificate);
                    return context.Response.WriteAsync("hello world");
                },
                serviceContext, _serverAddress))
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

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task HttpsSchemePassedToRequestFeature()
        {
            var serviceContext = new TestServiceContext(
                new HttpsConnectionFilter(
                    new HttpsConnectionFilterOptions
                    {
                        ServerCertificate = _x509Certificate2
                    },
                    new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context => context.Response.WriteAsync(context.Request.Scheme), serviceContext, _serverAddress))
            {
                var result = await HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
                Assert.Equal("https", result);
            }
        }

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task DoesNotSupportTls10()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                    ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context => context.Response.WriteAsync("hello world"), serviceContext, _serverAddress))
            {
                // SslStream is used to ensure the certificate is actually passed to the server
                // HttpClient might not send the certificate because it is invalid or it doesn't match any
                // of the certificate authorities sent by the server in the SSL handshake.
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
                    var ex = await Assert.ThrowsAsync(typeof(IOException),
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
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
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
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context => TaskCache.CompletedTask, serviceContext, _serverAddress))
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
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = mode,
                    ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => false
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context => TaskCache.CompletedTask, serviceContext, _serverAddress))
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
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = mode,
                },
                new NoOpConnectionFilter())
            );

            using (var server = new TestServer(context => TaskCache.CompletedTask, serviceContext, _serverAddress))
            {
                using (var client = new TcpClient())
                {
                    var stream = await OpenSslStream(client, server);
                    await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    await AssertConnectionResult(stream, false);
                }
            }
        }

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task CertificatePassedToHttpContextIsNotDisposed()
        {
            var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                new HttpsConnectionFilterOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                    ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true
                },
                new NoOpConnectionFilter())
                );

            RequestDelegate app = context =>
            {
                var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tlsFeature);
                Assert.NotNull(tlsFeature.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate.PublicKey);
                return context.Response.WriteAsync("hello world");
            };

            using (var server = new TestServer(app, serviceContext, _serverAddress))
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
