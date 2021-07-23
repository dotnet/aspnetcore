// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    public class WebHostTests : LoggedTest
    {
        [ConditionalFact]
        [MsQuicSupported]
        public async Task UseUrls_HelloWorld_ClientSuccess()
        {
            // Arrange
            var builder = GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.ConfigureEndpointDefaults(listenOptions =>
                            {
                                listenOptions.Protocols = Core.HttpProtocols.Http3;
                            });
                        })
                        .UseUrls("https://127.0.0.1:0")
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
                                await context.Response.WriteAsync("hello, world");
                            });
                        });
                })
                .ConfigureServices(AddTestLogging);

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                await host.StartAsync();

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request.Version = HttpVersion.Version30;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var response = await client.SendAsync(request);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version30, response.Version);
                var responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("hello, world", responseText);

                await host.StopAsync();
            }
        }

        [ConditionalTheory]
        [MsQuicSupported]
        [InlineData(5002, 5003)]
        [InlineData(5004, 5004)]
        public async Task Listen_Http3AndSocketsCoexistOnDifferentEndpoints_ClientSuccess(int http3Port, int http1Port)
        {
            // Arrange
            var builder = GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.Listen(IPAddress.Parse("127.0.0.1"), http3Port, listenOptions =>
                            {
                                listenOptions.Protocols = Core.HttpProtocols.Http3;
                                listenOptions.UseHttps();
                            });
                            o.Listen(IPAddress.Parse("127.0.0.1"), http1Port, listenOptions =>
                            {
                                listenOptions.Protocols = Core.HttpProtocols.Http1;
                                listenOptions.UseHttps();
                            });
                        })
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
                                await context.Response.WriteAsync("hello, world");
                            });
                        });
                })
                .ConfigureServices(AddTestLogging);

            using var host = builder.Build();
            await host.StartAsync().DefaultTimeout();

            await CallHttp3AndHttp1EndpointsAsync(http3Port, http1Port);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task Listen_Http3AndSocketsCoexistOnSameEndpoint_ClientSuccess()
        {
            // Arrange
            var builder = GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.Listen(IPAddress.Parse("127.0.0.1"), 5005, listenOptions =>
                            {
                                listenOptions.Protocols = Core.HttpProtocols.Http1AndHttp2AndHttp3;
                                listenOptions.UseHttps();
                            });
                        })
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
                                await context.Response.WriteAsync("hello, world");
                            });
                        });
                })
                .ConfigureServices(AddTestLogging);

            using var host = builder.Build();
            await host.StartAsync().DefaultTimeout();

            await CallHttp3AndHttp1EndpointsAsync(http3Port: 5005, http1Port: 5005);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task Listen_Http3AndSocketsCoexistOnSameEndpoint_AltSvcEnabled_Upgrade()
        {
            // Arrange
            var builder = GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.EnableAltSvc = true;
                            o.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                            {
                                listenOptions.Protocols = Core.HttpProtocols.Http1AndHttp2AndHttp3;
                                listenOptions.UseHttps();
                            });
                        })
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
                                await context.Response.WriteAsync("hello, world");
                            });
                        });
                })
                .ConfigureServices(AddTestLogging);

            using var host = builder.Build();
            await host.StartAsync().DefaultTimeout();

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using (var client = new HttpClient(httpClientHandler))
            {
                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                var response1 = await client.SendAsync(request1).DefaultTimeout();

                // Assert
                response1.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version20, response1.Version);
                var responseText1 = await response1.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText1);

                Assert.True(response1.Headers.TryGetValues("alt-svc", out var altSvcValues));
                Assert.Single(altSvcValues, @$"h3="":{host.GetPort()}""; ma=84600");

                // Act
                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                var response2 = await client.SendAsync(request2).DefaultTimeout();

                // Assert
                response2.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version30, response2.Version);
                var responseText2 = await response2.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText2);

                Assert.False(response2.Headers.Contains("alt-svc"));
            }

            await host.StopAsync().DefaultTimeout();
        }

        private static async Task CallHttp3AndHttp1EndpointsAsync(int http3Port, int http1Port)
        {
            // HTTP/3
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{http3Port}/");
                request.Version = HttpVersion.Version30;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var response = await client.SendAsync(request).DefaultTimeout();

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version30, response.Version);
                var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText);
            }

            // HTTP/1.1
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using (var client = new HttpClient(httpClientHandler))
            {
                // HTTP/1.1
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{http1Port}/");

                // Act
                var response = await client.SendAsync(request).DefaultTimeout();

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version11, response.Version);
                var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText);
            }
        }

        public static IHostBuilder GetHostBuilder(long? maxReadBufferSize = null)
        {
            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseQuic(options =>
                        {
                            options.MaxReadBufferSize = maxReadBufferSize;
                            options.Alpn = QuicTestHelpers.Alpn;
                            options.IdleTimeout = TimeSpan.FromSeconds(20);
                        });
                });
        }
    }
}
