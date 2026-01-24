// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "DirectSsl uses Linux epoll")]
public class DirectSslTransportTests : LoggedTestBase
{
    private static readonly string CertPath = Path.Combine(AppContext.BaseDirectory, "shared", "TestCertificates", "directssl-test.crt");
    private static readonly string KeyPath = Path.Combine(AppContext.BaseDirectory, "shared", "TestCertificates", "directssl-test.key");

    private HttpClient CreateClient()
    {
        return new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    [Fact]
    public async Task DirectSslTransport_SimpleRequest_ReturnsResponse()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelDirectSslTransport()
                    .UseDirectSslSockets(options =>
                    {
                        options.CertificatePath = CertPath;
                        options.PrivateKeyPath = KeyPath;
                        options.WorkerCount = 2;
                    })
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(context => context.Response.WriteAsync("Hello from DirectSsl!"));
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        await host.StartAsync();

        var port = host.GetPort();

        using var client = CreateClient();
        var response = await client.GetAsync($"https://localhost:{port}/");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from DirectSsl!", content);

        await host.StopAsync();
    }

    [Fact]
    public async Task DirectSslTransport_MultipleRequests_AllSucceed()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelDirectSslTransport()
                    .UseDirectSslSockets(options =>
                    {
                        options.CertificatePath = CertPath;
                        options.PrivateKeyPath = KeyPath;
                        options.WorkerCount = 2;
                    })
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync($"Request {context.Request.Path}");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        await host.StartAsync();

        var port = host.GetPort();

        using var client = CreateClient();

        // Make multiple requests
        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync($"https://localhost:{port}/path{i}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal($"Request /path{i}", content);
        }

        await host.StopAsync();
    }

    [Fact]
    public async Task DirectSslTransport_ConcurrentRequests_AllSucceed()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelDirectSslTransport()
                    .UseDirectSslSockets(options =>
                    {
                        options.CertificatePath = CertPath;
                        options.PrivateKeyPath = KeyPath;
                        options.WorkerCount = 4;
                    })
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await Task.Delay(10); // Simulate some work
                            await context.Response.WriteAsync("OK");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        await host.StartAsync();

        var port = host.GetPort();

        // Make concurrent requests from multiple clients
        var tasks = new List<Task<string>>();
        for (int i = 0; i < 20; i++)
        {
            var task = Task.Run(async () =>
            {
                using var client = CreateClient();
                var response = await client.GetAsync($"https://localhost:{port}/");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.Equal("OK", r));

        await host.StopAsync();
    }

    [Fact]
    public async Task DirectSslTransport_KeepAlive_ReuseConnection()
    {
        int requestCount = 0;

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelDirectSslTransport()
                    .UseDirectSslSockets(options =>
                    {
                        options.CertificatePath = CertPath;
                        options.PrivateKeyPath = KeyPath;
                        options.WorkerCount = 2;
                    })
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(context =>
                        {
                            Interlocked.Increment(ref requestCount);
                            return context.Response.WriteAsync($"Request #{requestCount}");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        await host.StartAsync();

        var port = host.GetPort();

        // Single client makes multiple requests (should reuse connection)
        using var client = CreateClient();
        for (int i = 1; i <= 5; i++)
        {
            var response = await client.GetAsync($"https://localhost:{port}/");
            response.EnsureSuccessStatusCode();
        }

        Assert.Equal(5, requestCount);

        await host.StopAsync();
    }

    [Fact]
    public async Task DirectSslTransport_PostRequest_ReadsBody()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelDirectSslTransport()
                    .UseDirectSslSockets(options =>
                    {
                        options.CertificatePath = CertPath;
                        options.PrivateKeyPath = KeyPath;
                        options.WorkerCount = 2;
                    })
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            using var reader = new StreamReader(context.Request.Body);
                            var body = await reader.ReadToEndAsync();
                            await context.Response.WriteAsync($"Received: {body}");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        await host.StartAsync();

        var port = host.GetPort();

        using var client = CreateClient();
        var content = new StringContent("Hello DirectSsl!");
        var response = await client.PostAsync($"https://localhost:{port}/", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("Received: Hello DirectSsl!", responseContent);

        await host.StopAsync();
    }

    [Fact]
    public async Task DirectSslTransport_LargeResponse_Succeeds()
    {
        var largeData = new string('X', 1024 * 1024); // 1 MB

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelDirectSslTransport()
                    .UseDirectSslSockets(options =>
                    {
                        options.CertificatePath = CertPath;
                        options.PrivateKeyPath = KeyPath;
                        options.WorkerCount = 2;
                    })
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(context => context.Response.WriteAsync(largeData));
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        await host.StartAsync();

        var port = host.GetPort();

        using var client = CreateClient();
        var response = await client.GetAsync($"https://localhost:{port}/");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(largeData.Length, content.Length);
        Assert.Equal(largeData, content);

        await host.StopAsync();
    }
}
