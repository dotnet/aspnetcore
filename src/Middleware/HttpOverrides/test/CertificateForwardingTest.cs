// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpOverrides;

public class CertificateForwardingTests
{
    [Fact]
    public void VerifySettingNullHeaderOptionThrows()
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddCertificateForwarding(o => o.CertificateHeader = null);
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<CertificateForwardingOptions>>();
        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void VerifySettingEmptyHeaderOptionThrows()
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddCertificateForwarding(o => o.CertificateHeader = "");
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<CertificateForwardingOptions>>();
        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public async Task VerifyHeaderIsUsedIfNoCertificateAlreadySet()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => { });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next(context);
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(context.Connection.ClientCertificate, Certificates.SelfSignedValidWithNoEku);
                        await next(context);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Client-Cert"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
        });
    }

    [Fact]
    public async Task VerifyHeaderOverridesCertificateEvenAlreadySet()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => { });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        context.Connection.ClientCertificate = Certificates.SelfSignedNotYetValid;
                        await next(context);
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(context.Connection.ClientCertificate, Certificates.SelfSignedValidWithNoEku);
                        await next(context);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Client-Cert"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
        });
    }

    [Fact]
    public async Task VerifySettingTheAzureHeaderOnTheForwarderOptionsWorks()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => options.CertificateHeader = "X-ARR-ClientCert");
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next(context);
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(context.Connection.ClientCertificate, Certificates.SelfSignedValidWithNoEku);
                        await next(context);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-ARR-ClientCert"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
        });
    }

    [Fact]
    public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => options.CertificateHeader = "some-random-header");
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next(context);
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next(context);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["not-the-right-header"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
        });
    }

    [Fact]
    public async Task VerifyArrHeaderEncodedCertFailsOnBadEncoding()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => { });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next(context);
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next(context);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Client-Cert"] = "OOPS" + Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
        });
    }
}
