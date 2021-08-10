// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            using var client = CreateClient();

            await host.StartAsync().DefaultTimeout();

            // Using localhost instead of 127.0.0.1 because IPs don't set SNI and the Host header isn't currently used as an override.
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:{host.GetPort()}/");
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            // https://github.com/dotnet/runtime/issues/57169 Host isn't used for SNI
            request.Headers.Host = "testhost";

            var response = await client.SendAsync(request, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpVersion.Version30, response.Version);
            Assert.Equal("Hello World", result);

            await host.StopAsync().DefaultTimeout();
        }

        private static HttpMessageInvoker CreateClient(TimeSpan? idleTimeout = null)
        {
            var handler = new SocketsHttpHandler();
            handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, __, ___, ____) => true,
                TargetHost = "targethost"
            };
            if (idleTimeout != null)
            {
                handler.PooledConnectionIdleTimeout = idleTimeout.Value;
            }

            return new HttpMessageInvoker(handler);
        }

        private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null)
        {
            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            if (configureKestrel == null)
                            {
                                o.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                                {
                                    listenOptions.Protocols = protocol ?? HttpProtocols.Http3;
                                    listenOptions.UseHttps();
                                });
                            }
                            else
                            {
                                configureKestrel(o);
                            }
                        })
                        .Configure(app =>
                        {
                            app.Run(requestDelegate);
                        });
                })
                .ConfigureServices(AddTestLogging)
                .ConfigureHostOptions(o =>
                {
                    if (Debugger.IsAttached)
                    {
                        // Avoid timeout while debugging.
                        o.ShutdownTimeout = TimeSpan.FromHours(1);
                    }
                    else
                    {
                        o.ShutdownTimeout = TimeSpan.FromSeconds(1);
                    }
                });
        }
    }
}
