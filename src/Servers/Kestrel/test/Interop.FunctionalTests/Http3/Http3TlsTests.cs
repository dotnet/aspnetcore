// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Interop.FunctionalTests.Http3;

[Collection(nameof(NoParallelCollection))]
public class Http3TlsTests : LoggedTest
{
    [ConditionalFact]
    [MsQuicSupported]
    public async Task ServerCertificateSelector_Invoked()
    {
        var serverCertificateSelectorActionCalled = false;
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
                        serverCertificateSelectorActionCalled = true;
                        Assert.Null(context); // The context isn't available durring the quic handshake.
                        Assert.Equal("testhost", host);
                        return TestResources.GetTestCertificate();
                    };
                });
            });
        });

        using var host = builder.Build();
        using var client = HttpHelpers.CreateClient();

        await host.StartAsync().DefaultTimeout();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        request.Headers.Host = "testhost";

        var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpVersion.Version30, response.Version);
        Assert.Equal("Hello World", result);

        Assert.True(serverCertificateSelectorActionCalled);

        await host.StopAsync().DefaultTimeout();
    }

    [ConditionalTheory]
    [InlineData(ClientCertificateMode.RequireCertificate)]
    [InlineData(ClientCertificateMode.AllowCertificate)]
    [MsQuicSupported]
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
            var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(() => sendTask).DefaultTimeout();
            Logger.LogInformation(ex, "SendAsync successfully threw error.");
        }
        else
        {
            // Because we can't verify the exact error reason, check that the cert is the cause by successfully
            // making a call when invalid certs are allowed.
            using var response = await sendTask.DefaultTimeout();
            response.EnsureSuccessStatusCode();
            Assert.Equal("True", await response.Content.ReadAsStringAsync().DefaultTimeout());
        }

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
        using var client = HttpHelpers.CreateClient(includeClientCert: false);

        await host.StartAsync().DefaultTimeout();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("False", await response.Content.ReadAsStringAsync());

        await host.StopAsync().DefaultTimeout();
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(HttpProtocols.Http3)]
    [InlineData(HttpProtocols.Http1AndHttp2AndHttp3)]
    public async Task OnAuthenticate_Available_Throws(HttpProtocols protocols)
    {
        await ServerRetryHelper.BindPortsWithRetry(async port =>
        {
            var builder = CreateHostBuilder(async context =>
            {
                await context.Response.WriteAsync("Hello World");
            }, configureKestrel: kestrelOptions =>
            {
                kestrelOptions.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = protocols;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
                        httpsOptions.OnAuthenticate = (_, _) => { };
                    });
                });
            });

            using var host = builder.Build();

            var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
                host.StartAsync().DefaultTimeout());
            Assert.Equal("The OnAuthenticate callback is not supported with HTTP/3.", exception.Message);
        }, Logger);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task TlsHandshakeCallbackOptions_Invoked()
    {
        var configuredState = new object();
        object callbackState = null;
        var builder = CreateHostBuilder(async context =>
        {
            await context.Response.WriteAsync("Hello World");
        }, configureKestrel: kestrelOptions =>
        {
            kestrelOptions.ListenAnyIP(0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http3;
                listenOptions.UseHttps(new TlsHandshakeCallbackOptions
                {
                    OnConnection = (context) =>
                    {
                        callbackState = context.State;
                        return ValueTask.FromResult(new SslServerAuthenticationOptions
                        {
                            ServerCertificate = TestResources.GetTestCertificate(),
                            ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
                        });
                    },
                    OnConnectionState = configuredState
                });
            });
        });

        using var host = builder.Build();
        using var client = HttpHelpers.CreateClient();

        await host.StartAsync().DefaultTimeout();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        request.Headers.Host = "testhost";

        var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpVersion.Version30, response.Version);
        Assert.Equal("Hello World", result);

        Assert.Equal(configuredState, callbackState);

        await host.StopAsync().DefaultTimeout();
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public async Task UseKestrelCore_CodeBased(bool useQuic, bool useHttps, bool useHttpsEnablesHttpsConfiguration)
    {
        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(0, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http3;
                        if (useHttps)
                        {
                            if (useHttpsEnablesHttpsConfiguration)
                            {
                                listenOptions.UseHttps(httpsOptions =>
                                {
                                    httpsOptions.ServerCertificate = TestResources.GetTestCertificate();
                                });
                            }
                            else
                            {
                                // Specifically choose an overload that doesn't enable https configuration
                                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                                {
                                    ServerCertificate = TestResources.GetTestCertificate()
                                });
                            }
                        }
                    });
                })
                .Configure(app => { });

        if (useQuic)
        {
            hostBuilder.UseQuic();
        }

        var host = hostBuilder.Build();

        if (useHttps && useHttpsEnablesHttpsConfiguration && useQuic)
        {
            // Binding succeeds
            await host.StartAsync();
            await host.StopAsync();
        }
        else
        {
            // This *could* work for `useHttps && !useHttpsEnablesHttpsConfiguration` if `UseQuic` implied `UseKestrelHttpsConfiguration`
            Assert.Throws<InvalidOperationException>(host.Run);
        }
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(true)]
    [InlineData(false)]
    public void UseKestrelCore_ConfigurationBased(bool useQuic)
    {
        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .ConfigureKestrel(serverOptions =>
                {
                    var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("Endpoints:end1:Url", "https://127.0.0.1:0"),
                        new KeyValuePair<string, string>("Endpoints:end1:Protocols", "Http3"),
                        new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx")),
                        new KeyValuePair<string, string>("Certificates:Default:Password", "testPassword"),
                    }).Build();
                    serverOptions.Configure(config);
                })
                .Configure(app => { });

        if (useQuic)
        {
            hostBuilder.UseQuic();
        }

        var host = hostBuilder.Build();

        // This *could* work (in some cases) if `UseQuic` implied `UseKestrelHttpsConfiguration`
        Assert.Throws<InvalidOperationException>(host.Run);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task LoadDevelopmentCertificateViaConfiguration()
    {
        var expectedCertificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
        var bytes = expectedCertificate.Export(X509ContentType.Pkcs12, "1234");
        var path = GetCertificatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Certificates:Development:Password", "1234"),
        }).Build();

        var ranConfigureKestrelAction = false;
        var ranUseHttpsAction = false;
        var hostBuilder = CreateHostBuilder(async context =>
        {
            await context.Response.WriteAsync("Hello World");
        }, configureKestrel: kestrelOptions =>
        {
            ranConfigureKestrelAction = true;
            kestrelOptions.Configure(config);

            kestrelOptions.ListenAnyIP(0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http3;
                listenOptions.UseHttps(_ =>
                {
                    ranUseHttpsAction = true;
                });
            });
        });

        Assert.False(ranConfigureKestrelAction);
        Assert.False(ranUseHttpsAction);

        using var host = hostBuilder.Build();
        await host.StartAsync().DefaultTimeout();

        Assert.True(ranConfigureKestrelAction);
        Assert.True(ranUseHttpsAction);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        request.Headers.Host = "testhost";

        var ranCertificateValidation = false;
        var httpHandler = new SocketsHttpHandler();
        httpHandler.SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (object _sender, X509Certificate actualCertificate, X509Chain _chain, SslPolicyErrors _sslPolicyErrors) =>
            {
                ranCertificateValidation = true;
                Assert.Equal(expectedCertificate.GetSerialNumberString(), actualCertificate.GetSerialNumberString());
                return true;
            },
            TargetHost = "targethost",
        };
        using var client = new HttpMessageInvoker(httpHandler);

        var response = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpVersion.Version30, response.Version);
        Assert.Equal("Hello World", result);

        Assert.True(ranCertificateValidation);

        await host.StopAsync().DefaultTimeout();
    }

    ///<remarks>
    /// This is something of a hack - we should actually be calling
    /// <see cref="Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader.TryGetCertificatePath"/>.
    /// </remarks>
    private static string GetCertificatePath()
    {
        var appData = Environment.GetEnvironmentVariable("APPDATA");
        var home = Environment.GetEnvironmentVariable("HOME");
        var basePath = appData != null ? Path.Combine(appData, "ASP.NET", "https") : null;
        basePath = basePath ?? (home != null ? Path.Combine(home, ".aspnet", "https") : null);
        return Path.Combine(basePath, $"{typeof(Http3TlsTests).Assembly.GetName().Name}.pfx");
    }

    private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null)
    {
        return HttpHelpers.CreateHostBuilder(AddTestLogging, requestDelegate, protocol, configureKestrel);
    }
}
