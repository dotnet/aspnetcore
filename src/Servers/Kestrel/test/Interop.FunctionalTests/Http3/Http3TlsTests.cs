// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Quic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Interop.FunctionalTests.Http3
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/35070")]
    public class Http3TlsTests : LoggedTest
    {
        [ConditionalFact]
        [MsQuicSupported]
        public async Task ServerCertificateSelector_Invoked()
        {
            var builder = CreateHostBuilder(async context =>
            {
                await context.Response.WriteAsync("Hello World");
            }, configureKestrel: kestrelOptions =>
            {
                kestrelOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificateSelector = (context, host) =>
                        {
                            Assert.Null(context); // The context isn't available durring the quic handshake.
                            Assert.Equal("localhost", host);
                            return TestResources.GetTestCertificate();
                        };
                    });
                });
            });

            using var host = builder.Build();
            using var client = Http3Helpers.CreateClient();

            await host.StartAsync().DefaultTimeout();

            // Using localhost instead of 127.0.0.1 because IPs don't set SNI and the Host header isn't currently used as an override.
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:{host.GetPort()}/");
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            // https://github.com/dotnet/runtime/issues/57169 Host isn't used for SNI
            request.Headers.Host = "testhost";

            var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpVersion.Version30, response.Version);
            Assert.Equal("Hello World", result);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [InlineData(ClientCertificateMode.RequireCertificate)]
        [InlineData(ClientCertificateMode.AllowCertificate)]
        [MsQuicSupported]
        public async Task ClientCertificate_AllowOrRequire_Available_Accepted(ClientCertificateMode mode)
        {
            var builder = CreateHostBuilder(async context =>
            {
                var hasCert = context.Connection.ClientCertificate != null;
                await context.Response.WriteAsync(hasCert.ToString());
            }, configureKestrel: kestrelOptions =>
            {
                kestrelOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
                        httpsOptions.ClientCertificateMode = mode;
                        httpsOptions.AllowAnyClientCertificate();
                    });
                });
            });

            using var host = builder.Build();
            using var client = Http3Helpers.CreateClient(includeClientCert: true);

            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpVersion.Version30, response.Version);
            Assert.Equal("True", result);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [InlineData(ClientCertificateMode.NoCertificate)]
        [InlineData(ClientCertificateMode.DelayCertificate)]
        [MsQuicSupported]
        public async Task ClientCertificate_NoOrDelayed_Available_Ignored(ClientCertificateMode mode)
        {
            var builder = CreateHostBuilder(async context =>
            {
                var hasCert = context.Connection.ClientCertificate != null;
                await context.Response.WriteAsync(hasCert.ToString());
            }, configureKestrel: kestrelOptions =>
            {
                kestrelOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
                        httpsOptions.ClientCertificateMode = mode;
                        httpsOptions.AllowAnyClientCertificate();
                    });
                });
            });

            using var host = builder.Build();
            using var client = Http3Helpers.CreateClient(includeClientCert: true);

            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpVersion.Version30, response.Version);
            Assert.Equal("False", result);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [InlineData(ClientCertificateMode.RequireCertificate)]
        [InlineData(ClientCertificateMode.AllowCertificate)]
        [MsQuicSupported]
        public async Task ClientCertificate_AllowOrRequire_Available_Invalid_Refused(ClientCertificateMode mode)
        {
            var builder = CreateHostBuilder(async context =>
            {
                var hasCert = context.Connection.ClientCertificate != null;
                await context.Response.WriteAsync(hasCert.ToString());
            }, configureKestrel: kestrelOptions =>
            {
                kestrelOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
                        httpsOptions.ClientCertificateMode = mode;
                        // httpsOptions.AllowAnyClientCertificate(); // The self-signed cert is invalid. Let it fail the default checks.
                    });
                });
            });

            using var host = builder.Build();
            using var client = Http3Helpers.CreateClient(includeClientCert: true);

            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken.None).DefaultTimeout());
            // This poor error is likely a symptom of https://github.com/dotnet/runtime/issues/57246
            // QuicListener returns the connection before (or in spite of) the cert validation failing.
            // There's a race where the stream could be accepted before the connection is aborted.
            var qex = Assert.IsType<QuicOperationAbortedException>(ex.InnerException);
            Assert.Equal("Operation aborted.", qex.Message);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task ClientCertificate_Allow_NotAvailable_Optional()
        {
            var builder = CreateHostBuilder(async context =>
            {
                var hasCert = context.Connection.ClientCertificate != null;
                await context.Response.WriteAsync(hasCert.ToString());
            }, configureKestrel: kestrelOptions =>
            {
                kestrelOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
                        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                        httpsOptions.AllowAnyClientCertificate();
                    });
                });
            });

            using var host = builder.Build();
            using var client = Http3Helpers.CreateClient(includeClientCert: false);

            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // https://github.com/dotnet/runtime/issues/57308, optional client certs aren't supported.
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken.None).DefaultTimeout());
            Assert.StartsWith("Connection has been shutdown by transport. Error Code: 0x80410100", ex.Message);

            await host.StopAsync().DefaultTimeout();
        }

        private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null)
        {
            return Http3Helpers.CreateHostBuilder(AddTestLogging, requestDelegate, protocol, configureKestrel);
        }
    }
}
