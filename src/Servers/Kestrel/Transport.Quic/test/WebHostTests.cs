// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Quic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[Collection(nameof(NoParallelCollection))]
public class WebHostTests : LoggedTest
{
    // This test isn't conditional on QuicListener.IsSupported. Instead, it verifies that HTTP/3 runs on expected CI platforms:
    // 1. Windows 11 or later.
    // 2. Linux with libmsquic package installed.
    //
    // The main build and PR builds run Helix tests run on different OSes. Be cautious when editing OSes skipped on this test
    // as the test might pass in the PR build but cause the main build to fail once merged.
    [ConditionalFact]
    [SkipNonHelix]
    [SkipOnAlpine("https://github.com/dotnet/aspnetcore/issues/46537")]
    [SkipOnMariner("https://github.com/dotnet/aspnetcore/issues/46537")]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "HTTP/3 isn't supported on MacOS.")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win11_21H2)]
    public void HelixPlatform_QuicListenerIsSupported()
    {
        Assert.True(QuicListener.IsSupported, "QuicListener.IsSupported should be true.");
        Assert.True(new MsQuicSupportedAttribute().IsMet, "MsQuicSupported.IsMet should be true.");
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task UseUrls_HelloWorld_ClientSuccess()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ConfigureEndpointDefaults(listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http3;
                        });
                        o.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
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
        using (var client = CreateClient())
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
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.Listen(IPAddress.Parse("127.0.0.1"), http3Port, listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http3;
                            listenOptions.UseHttps(TestResources.GetTestCertificate());
                        });
                        o.Listen(IPAddress.Parse("127.0.0.1"), http1Port, listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http1;
                            listenOptions.UseHttps(TestResources.GetTestCertificate());
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
    public async Task Listen_Http3AndSocketsOnDynamicEndpoint_Http3Disabled()
    {
        // Arrange
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http1AndHttp2AndHttp3;
                            listenOptions.UseHttps(TestResources.GetTestCertificate());
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

        Assert.Contains(TestSink.Writes, w => w.Message == CoreStrings.DynamicPortOnMultipleTransportsNotSupported);

        await host.StopAsync().DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task Listen_Http3AndSocketsCoexistOnSameEndpoint_ClientSuccess()
    {
        await ServerRetryHelper.BindPortsWithRetry(async port =>
        {
            // Arrange
            var builder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.Listen(IPAddress.Parse("127.0.0.1"), port, listenOptions =>
                            {
                                listenOptions.Protocols = Core.HttpProtocols.Http1AndHttp2AndHttp3;
                                listenOptions.UseHttps(TestResources.GetTestCertificate());
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

            await CallHttp3AndHttp1EndpointsAsync(http3Port: port, http1Port: port);

            await host.StopAsync().DefaultTimeout();
        }, Logger);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task Listen_Http3AndSocketsCoexistOnSameEndpoint_AltSvcEnabled_Upgrade()
    {
        await ServerRetryHelper.BindPortsWithRetry(async port =>
        {
            // Arrange
            var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.Listen(IPAddress.Parse("127.0.0.1"), port, listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http1AndHttp2AndHttp3;
                            listenOptions.UseHttps(TestResources.GetTestCertificate());
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

            using (var client = CreateClient())
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

                Assert.True(response1.Headers.TryGetValues("alt-svc", out var altSvcValues1));
                Assert.Single(altSvcValues1, @$"h3="":{host.GetPort()}""");

                // Act
                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                var response2 = await client.SendAsync(request2).DefaultTimeout();

                // Assert
                response2.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version30, response2.Version);
                var responseText2 = await response2.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText2);

                Assert.True(response2.Headers.TryGetValues("alt-svc", out var altSvcValues2));
                Assert.Single(altSvcValues2, @$"h3="":{host.GetPort()}""");
            }

            await host.StopAsync().DefaultTimeout();
        }, Logger);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task Listen_Http3AndSocketsCoexistOnSameEndpoint_AltSvcDisabled_NoUpgrade()
    {
        await ServerRetryHelper.BindPortsWithRetry(async port =>
        {
            // Arrange
            var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ConfigureEndpointDefaults(listenOptions =>
                        {
                            listenOptions.DisableAltSvcHeader = true;
                        });
                        o.Listen(IPAddress.Parse("127.0.0.1"), port, listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http1AndHttp2AndHttp3;
                            listenOptions.UseHttps(TestResources.GetTestCertificate());
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

            using (var client = CreateClient())
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

                Assert.False(response1.Headers.Contains("alt-svc"));

                // Act
                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                var response2 = await client.SendAsync(request2).DefaultTimeout();

                // Assert
                response2.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version20, response2.Version);
                var responseText2 = await response2.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText2);

                Assert.False(response2.Headers.Contains("alt-svc"));
            }

            await host.StopAsync().DefaultTimeout();
        }, Logger);
    }

    private static async Task CallHttp3AndHttp1EndpointsAsync(int http3Port, int http1Port)
    {
        using (var client = CreateClient())
        {
            // HTTP/3
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{http3Port}/");
            request1.Version = HttpVersion.Version30;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var response1 = await client.SendAsync(request1).DefaultTimeout();

            // Assert
            response1.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response1.Version);
            var responseText1 = await response1.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText1);

            // HTTP/1.1
            var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{http1Port}/");

            // Act
            var response2 = await client.SendAsync(request2).DefaultTimeout();

            // Assert
            response2.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version11, response2.Version);
            var responseText2 = await response2.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText2);
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StartAsync_Http3WithNonIPListener_ThrowError()
    {
        // Arrange
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenUnixSocket("/test-path", listenOptions =>
                        {
                            listenOptions.Protocols = Core.HttpProtocols.Http3;
                            listenOptions.UseHttps(TestResources.GetTestCertificate());
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

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync()).DefaultTimeout();

        // Assert
        Assert.Equal("No registered IMultiplexedConnectionListenerFactory supports endpoint UnixDomainSocketEndPoint: /test-path", ex.Message);
    }

    private static HttpClient CreateClient()
    {
        var httpHandler = new HttpClientHandler();
        httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        return new HttpClient(httpHandler);
    }
}
