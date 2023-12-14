// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration;

[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class IISMiddlewareTests
{
    [Fact]
    public async Task MiddlewareSkippedIfTokenIsMissing()
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(context =>
                        {
                            var auth = context.Features.Get<IHttpAuthenticationFeature>();
                            Assert.Null(auth);
                            assertsExecuted = true;
                            return Task.FromResult(0);
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "");
        req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        var response = await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MiddlewareRejectsRequestIfTokenHeaderIsMissing()
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(context =>
                        {
                            var auth = context.Features.Get<IHttpAuthenticationFeature>();
                            Assert.Null(auth);
                            assertsExecuted = true;
                            return Task.FromResult(0);
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "");
        var response = await server.CreateClient().SendAsync(req);
        Assert.False(assertsExecuted);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/", "/iisintegration", "shutdown")]
    [InlineData("/", "/iisintegration", "Shutdown")]
    [InlineData("/pathBase", "/pathBase/iisintegration", "shutdown")]
    [InlineData("/pathBase", "/pathBase/iisintegration", "Shutdown")]
    public async Task MiddlewareShutsdownGivenANCMShutdown(string pathBase, string requestPath, string shutdownEvent)
    {
        var requestExecuted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applicationStoppingFired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", pathBase)
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                        appLifetime.ApplicationStopping.Register(() => applicationStoppingFired.SetResult());

                        app.Run(context =>
                        {
                            requestExecuted.SetResult();
                            return Task.CompletedTask;
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, requestPath);
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-EVENT", shutdownEvent);
        var response = await server.CreateClient().SendAsync(request);

        await applicationStoppingFired.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        Assert.False(requestExecuted.Task.IsCompleted);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    public static TheoryData<HttpMethod> InvalidShutdownMethods
    {
        get
        {
            return new TheoryData<HttpMethod>
                {
                    HttpMethod.Put,
                    HttpMethod.Trace,
                    HttpMethod.Head,
                    HttpMethod.Get,
                    HttpMethod.Delete,
                    HttpMethod.Options
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidShutdownMethods))]
    public async Task MiddlewareIgnoresShutdownGivenWrongMethod(HttpMethod method)
    {
        var requestExecuted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applicationStoppingFired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                        appLifetime.ApplicationStopping.Register(() => applicationStoppingFired.SetResult());

                        app.Run(context =>
                        {
                            requestExecuted.SetResult();
                            return Task.CompletedTask;
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var request = new HttpRequestMessage(method, "/iisintegration");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-EVENT", "shutdown");
        var response = await server.CreateClient().SendAsync(request);

        Assert.False(applicationStoppingFired.Task.IsCompleted);
        await requestExecuted.Task.TimeoutAfter(TimeSpan.FromSeconds(2));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/path")]
    [InlineData("/path/iisintegration")]
    public async Task MiddlewareIgnoresShutdownGivenWrongPath(string path)
    {
        var requestExecuted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applicationStoppingFired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                        appLifetime.ApplicationStopping.Register(() => applicationStoppingFired.SetResult());

                        app.Run(context =>
                        {
                            requestExecuted.SetResult();
                            return Task.CompletedTask;
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-EVENT", "shutdown");
        var response = await server.CreateClient().SendAsync(request);

        Assert.False(applicationStoppingFired.Task.IsCompleted);
        await requestExecuted.Task.TimeoutAfter(TimeSpan.FromSeconds(2));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("event")]
    [InlineData("")]
    [InlineData(null)]
    public async Task MiddlewareIgnoresShutdownGivenWrongEvent(string shutdownEvent)
    {
        var requestExecuted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applicationStoppingFired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                        appLifetime.ApplicationStopping.Register(() => applicationStoppingFired.SetResult());

                        app.Run(context =>
                        {
                            requestExecuted.SetResult();
                            return Task.CompletedTask;
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/iisintegration");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-EVENT", shutdownEvent);
        var response = await server.CreateClient().SendAsync(request);

        Assert.False(applicationStoppingFired.Task.IsCompleted);
        await requestExecuted.Task.TimeoutAfter(TimeSpan.FromSeconds(2));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UrlDelayRegisteredAndPreferHostingUrlsSet()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(context => Task.FromResult(0));
                    });

                Assert.Null(webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey));
                Assert.Null(webHostBuilder.GetSetting(WebHostDefaults.PreferHostingUrlsKey));

                webHostBuilder.UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var configuration = host.Services.GetService<IConfiguration>();

        Assert.Equal("http://127.0.0.1:12345", configuration[WebHostDefaults.ServerUrlsKey]);
        Assert.Equal("true", configuration[WebHostDefaults.PreferHostingUrlsKey]);
    }

    [Fact]
    public async Task PathBaseHiddenFromServer()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/pathBase")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(context => Task.FromResult(0));
                    })
                    .UseTestServer();
            })
            .Build();

        host.GetTestServer();

        await host.StartAsync();

        var configuration = host.Services.GetService<IConfiguration>();
        Assert.Equal("http://127.0.0.1:12345", configuration[WebHostDefaults.ServerUrlsKey]);
    }

    [Fact]
    public async Task AddsUsePathBaseMiddlewareWhenPathBaseSpecified()
    {
        var requestPathBase = string.Empty;
        var requestPath = string.Empty;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/pathbase")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(context =>
                        {
                            requestPathBase = context.Request.PathBase.Value;
                            requestPath = context.Request.Path.Value;
                            return Task.FromResult(0);
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/PathBase/Path");
        request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        var response = await server.CreateClient().SendAsync(request);

        Assert.Equal("/PathBase", requestPathBase);
        Assert.Equal("/Path", requestPath);
    }

    [Fact]
    public async Task AddsAuthenticationHandlerByDefault()
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                            var windows = await auth.GetSchemeAsync(IISDefaults.AuthenticationScheme);
                            Assert.NotNull(windows);
                            Assert.Null(windows.DisplayName);
                            Assert.Equal("Microsoft.AspNetCore.Server.IISIntegration.AuthenticationHandler", windows.HandlerType.FullName);
                            assertsExecuted = true;
                        });
                    })
                    .UseTestServer();
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "");
        req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        await server.CreateClient().SendAsync(req);

        Assert.True(assertsExecuted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnlyAddAuthenticationHandlerIfForwardWindowsAuthentication(bool forward)
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var auth = context.RequestServices.GetService<IAuthenticationSchemeProvider>();
                            Assert.NotNull(auth);
                            var windowsAuth = await auth.GetSchemeAsync(IISDefaults.AuthenticationScheme);
                            if (forward)
                            {
                                Assert.NotNull(windowsAuth);
                                Assert.Null(windowsAuth.DisplayName);
                                Assert.Equal("AuthenticationHandler", windowsAuth.HandlerType.Name);
                            }
                            else
                            {
                                Assert.Null(windowsAuth);
                            }
                            assertsExecuted = true;
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.Configure<IISOptions>(options =>
                {
                    options.ForwardWindowsAuthentication = forward;
                });
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "");
        req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        await server.CreateClient().SendAsync(req);

        Assert.True(assertsExecuted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DoesNotBlowUpWithoutAuth(bool forward)
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting("TOKEN", "TestToken")
                    .UseSetting("PORT", "12345")
                    .UseSetting("APPL_PATH", "/")
                    .UseIISIntegration()
                    .Configure(app =>
                    {
                        app.Run(context =>
                        {
                            assertsExecuted = true;
                            return Task.FromResult(0);
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.Configure<IISOptions>(options =>
                {
                    options.ForwardWindowsAuthentication = forward;
                });
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "");
        req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
        await server.CreateClient().SendAsync(req);

        Assert.True(assertsExecuted);
    }
}
