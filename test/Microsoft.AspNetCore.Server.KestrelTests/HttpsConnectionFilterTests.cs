// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class HttpsConnectionFilterTests
    {
        private async Task App(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            response.Headers.Clear();
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

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanReadAndWriteWithHttpsConnectionFilter()
        {
            RemoteCertificateValidationCallback validationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
#if DNX451
                var handler = new HttpClientHandler();
                ServicePointManager.ServerCertificateValidationCallback += validationCallback;
#else
                var handler = new WinHttpHandler();
                handler.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif
                var serverAddress = $"https://localhost:{TestServer.GetNextPort()}/";
                var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                        new HttpsConnectionFilterOptions
                        { ServerCertificate = new X509Certificate2(@"TestResources/testCert.pfx", "testPassword") },
                        new NoOpConnectionFilter())
                );

                using (var server = new TestServer(App, serviceContext, serverAddress))
                {
                    using (var client = new HttpClient(handler))
                    {
                        var result = await client.PostAsync(serverAddress, new FormUrlEncodedContent(new[] {
                            new KeyValuePair<string, string>("content", "Hello World?")
                        }));

                        Assert.Equal("content=Hello+World%3F", await result.Content.ReadAsStringAsync());
                    }
                }
            }
            finally
            {
#if DNX451
                ServicePointManager.ServerCertificateValidationCallback -= validationCallback;
#endif
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task RequireCertificateFailsWhenNoCertificate()
        {
            RemoteCertificateValidationCallback validationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
#if DNX451
                var handler = new HttpClientHandler();
                ServicePointManager.ServerCertificateValidationCallback += validationCallback;
#else
                var handler = new WinHttpHandler();
                handler.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

                var serverAddress = $"https://localhost:{TestServer.GetNextPort()}/";
                var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                        new HttpsConnectionFilterOptions
                        {
                            ServerCertificate = new X509Certificate2(@"TestResources/testCert.pfx", "testPassword"),
                            ClientCertificateMode = ClientCertificateMode.RequireCertificate
                        },
                        new NoOpConnectionFilter())
                );

                using (var server = new TestServer(App, serviceContext, serverAddress))
                {
                    using (var client = new HttpClient(handler))
                    {
                        await Assert.ThrowsAnyAsync<Exception>(
                            () => client.GetAsync(serverAddress));
                    }
                }
            }
            finally
            {
#if DNX451
                ServicePointManager.ServerCertificateValidationCallback -= validationCallback;
#endif
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task AllowCertificateContinuesWhenNoCertificate()
        {
            RemoteCertificateValidationCallback validationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
#if DNX451
                var handler = new HttpClientHandler();
                ServicePointManager.ServerCertificateValidationCallback += validationCallback;
#else
                var handler = new WinHttpHandler();
                handler.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

                var serverAddress = $"https://localhost:{TestServer.GetNextPort()}/";
                var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                    new HttpsConnectionFilterOptions
                    {
                        ServerCertificate = new X509Certificate2(@"TestResources/testCert.pfx", "testPassword"),
                        ClientCertificateMode = ClientCertificateMode.AllowCertificate
                    },
                    new NoOpConnectionFilter())
                );

                RequestDelegate app = context =>
                {
                    Assert.Equal(context.Features.Get<ITlsConnectionFeature>(), null);
                    return context.Response.WriteAsync("hello world");
                };

                using (var server = new TestServer(app, serviceContext, serverAddress))
                {
                    using (var client = new HttpClient(handler))
                    {
                        var result = await client.GetAsync(serverAddress);

                        Assert.Equal("hello world", await result.Content.ReadAsStringAsync());
                    }
                }
            }
            finally
            {
#if DNX451
                ServicePointManager.ServerCertificateValidationCallback -= validationCallback;
#endif
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CertificatePassedToHttpContext()
        {
            RemoteCertificateValidationCallback validationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
#if DNX451
                ServicePointManager.ServerCertificateValidationCallback += validationCallback;
#endif

                var serverAddress = $"https://localhost:{TestServer.GetNextPort()}/";
                var serviceContext = new TestServiceContext(new HttpsConnectionFilter(
                    new HttpsConnectionFilterOptions
                    {
                        ServerCertificate = new X509Certificate2(@"TestResources/testCert.pfx", "testPassword"),
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
                    return context.Response.WriteAsync("hello world");
                };

                using (var server = new TestServer(app, serviceContext, serverAddress))
                {
                    // SslStream is used to ensure the certificate is actually passed to the server
                    // HttpClient might not send the certificate because it is invalid or it doesn't match any
                    // of the certificate authorities sent by the server in the SSL handshake.
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync("127.0.0.1", server.Port);

                        SslStream stream = new SslStream(client.GetStream(), false, (sender, certificate, chain, errors) => true,
                            (sender, host, certificates, certificate, issuers) => new X509Certificate2(@"TestResources/testCert.pfx", "testPassword"));
                        await stream.AuthenticateAsClientAsync("localhost");

                        var request = Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\n\r\n");
                        await stream.WriteAsync(request, 0, request.Length);

                        var reader = new StreamReader(stream);
                        var line = await reader.ReadLineAsync();
                        Assert.Equal("HTTP/1.0 200 OK", line);
                    }
                }
            }
            finally
            {
#if DNX451
                ServicePointManager.ServerCertificateValidationCallback -= validationCallback;
#endif
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task HttpsSchemePassedToRequestFeature()
        {
            RemoteCertificateValidationCallback validationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
#if DNX451
                var handler = new HttpClientHandler();
                ServicePointManager.ServerCertificateValidationCallback += validationCallback;
#else
                var handler = new WinHttpHandler();
                handler.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

                var serverAddress = $"https://localhost:{TestServer.GetNextPort()}/";
                var serviceContext = new TestServiceContext(
                    new HttpsConnectionFilter(
                        new HttpsConnectionFilterOptions
                        {
                            ServerCertificate = new X509Certificate2(@"TestResources/testCert.pfx", "testPassword")
                        },
                        new NoOpConnectionFilter())
                );

                RequestDelegate app = context => context.Response.WriteAsync(context.Request.Scheme);

                using (var server = new TestServer(app, serviceContext, serverAddress))
                {
                    using (var client = new HttpClient(handler))
                    {
                        var result = await client.GetAsync(serverAddress);

                        Assert.Equal("https", await result.Content.ReadAsStringAsync());
                    }
                }
            }
            finally
            {
#if DNX451
                ServicePointManager.ServerCertificateValidationCallback -= validationCallback;
#endif
            }
        }
    }
}
