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
using Microsoft.Extensions.Logging;
using Xunit;

namespace Interop.FunctionalTests.Http3;

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
        using var client = HttpHelpers.CreateClient();

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
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "https://github.com/dotnet/aspnetcore/issues/35800")]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2,
        SkipReason = "Windows versions newer than 20H2 do not enable TLS 1.1: https://github.com/dotnet/aspnetcore/issues/37761")]
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
        using var client = HttpHelpers.CreateClient(includeClientCert: true);

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
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "https://github.com/dotnet/aspnetcore/issues/35800")]
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
        using var client = HttpHelpers.CreateClient(includeClientCert: true);

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
    [InlineData(ClientCertificateMode.RequireCertificate, false)]
    [InlineData(ClientCertificateMode.RequireCertificate, true)]
    [InlineData(ClientCertificateMode.AllowCertificate, false)]
    [InlineData(ClientCertificateMode.AllowCertificate, true)]
    [MsQuicSupported]
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "https://github.com/dotnet/aspnetcore/issues/35800")]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/35070")]
    public async Task ClientCertificate_AllowOrRequire_Available_Invalid_Refused(ClientCertificateMode mode, bool serverAllowInvalid)
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

                    if (serverAllowInvalid)
                    {
                        httpsOptions.AllowAnyClientCertificate(); // The self-signed cert is invalid. Let it fail the default checks.
                    }
                });
            });
        });

        using var host = builder.Build();
        using var client = HttpHelpers.CreateClient(includeClientCert: true);

        await host.StartAsync().DefaultTimeout();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var sendTask = client.SendAsync(request, CancellationToken.None);

        if (!serverAllowInvalid)
        {
            // In .NET 6 there is a race condition between throwing HttpRequestException and QuicException.
            // Unable to test the exact error.
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => sendTask).DefaultTimeout();
            Logger.LogInformation(ex, "SendAsync successfully threw error.");
        }
        else
        {
            // Because we can't verify the exact error reason, check that the cert is the cause be successfully
            // making a call when invalid certs are allowed.
            var response = await sendTask.DefaultTimeout();
            response.EnsureSuccessStatusCode();
        }

        await host.StopAsync().DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "https://github.com/dotnet/aspnetcore/issues/35800")]
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
        using var client = HttpHelpers.CreateClient(includeClientCert: false);

        await host.StartAsync().DefaultTimeout();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        // https://github.com/dotnet/runtime/issues/57308, optional client certs aren't supported.
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken.None).DefaultTimeout());
        Assert.StartsWith("Connection has been shutdown by transport.", ex.Message);

        await host.StopAsync().DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task OnAuthentice_Available_Throws()
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
                    httpsOptions.OnAuthenticate = (_, _) => { };
                });
            });
        });

        using var host = builder.Build();
        using var client = HttpHelpers.CreateClient();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            host.StartAsync().DefaultTimeout());
        Assert.Equal("The OnAuthenticate callback is not supported with HTTP/3.", exception.Message);
    }

    private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null)
    {
        return HttpHelpers.CreateHostBuilder(AddTestLogging, requestDelegate, protocol, configureKestrel);
    }
}
