// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks;

public class HealthCheckMiddlewareTests
{
    [Fact]
    public void ThrowFriendlyErrorWhenServicesNotRegistered()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                });
            }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => host.Start());

        Assert.Equal(
            "Unable to find the required services. Please add all the required services by calling " +
            "'IServiceCollection.AddHealthChecks' inside the call to 'ConfigureServices(...)' " +
            "in the application startup code.",
            ex.Message);
    }

    [Fact] // Matches based on '.Map'
    public async Task IgnoresRequestThatDoesNotMatchPath()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/frob");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // Matches based on '.Map'
    public async Task MatchIsCaseInsensitive()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/HEALTH");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsPlainTextStatus()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StatusCodeIs200IfNoChecks()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StatusCodeIs200IfAllChecksHealthy()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddCheck("Foo", () => HealthCheckResult.Healthy("A-ok!"))
                        .AddCheck("Bar", () => HealthCheckResult.Healthy("A-ok!"))
                        .AddCheck("Baz", () => HealthCheckResult.Healthy("A-ok!"));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StatusCodeIs200IfCheckIsDegraded()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddCheck("Foo", () => HealthCheckResult.Healthy("A-ok!"))
                        .AddCheck("Bar", () => HealthCheckResult.Degraded("Not so great."))
                        .AddCheck("Baz", () => HealthCheckResult.Healthy("A-ok!"));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Degraded", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StatusCodeIs503IfCheckIsUnhealthy()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")))
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Unhealthy("Pretty bad.")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StatusCodeIs503IfCheckHasUnhandledException()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")))
                        .AddAsyncCheck("Bar", () => throw null)
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanUseCustomWriter()
    {
        var expectedJson = JsonConvert.SerializeObject(new
        {
            status = "Unhealthy",
        });

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResponseWriter = (c, r) =>
                        {
                            var json = JsonConvert.SerializeObject(new { status = r.Status.ToString(), });
                            c.Response.ContentType = "application/json";
                            return c.Response.WriteAsync(json);
                        },
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")))
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Unhealthy("Pretty bad.")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal("application/json", response.Content.Headers.ContentType.ToString());

        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task NoResponseWriterReturnsEmptyBody()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResponseWriter = null,
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")))
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Unhealthy("Pretty bad.")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanSetCustomStatusCodes()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResultStatusCodes =
                        {
                                [HealthStatus.Healthy] = 201,
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SetsCacheHeaders()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        Assert.Equal("no-store, no-cache", response.Headers.CacheControl.ToString());
        Assert.Equal("no-cache", response.Headers.Pragma.ToString());
        Assert.Equal(new string[] { "Thu, 01 Jan 1970 00:00:00 GMT" }, response.Content.Headers.GetValues(HeaderNames.Expires));
    }

    [Fact]
    public async Task CanSuppressCacheHeaders()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        AllowCachingResponses = true,
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Headers.CacheControl);
        Assert.Empty(response.Headers.Pragma.ToString());
        Assert.False(response.Content.Headers.Contains(HeaderNames.Expires));
    }

    [Fact]
    public async Task CanFilterChecks()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Name == "Foo" || check.Name == "Baz",
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")))
                        // Will get filtered out
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Unhealthy("A-ok!")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Healthy("A-ok!")));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenWithoutPath_AcceptsRequest()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks(default);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenWithPath_AcceptsRequestWithExtraSlash()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CanListenWithPath_AcceptsRequestWithCaseInsensitiveMatch()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/HEALTH");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenWithPath_RejectsRequestWithExtraSegments()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health/detailed");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // See: https://github.com/aspnet/Diagnostics/issues/511
    [Fact]
    public async Task CanListenWithPath_MultipleMiddleware_LeastSpecificFirst()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    // Throws if used
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResponseWriter = (c, r) => throw null,
                    });

                    app.UseHealthChecks("/health/detailed");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health/detailed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // See: https://github.com/aspnet/Diagnostics/issues/511
    [Fact]
    public async Task CanListenWithPath_MultipleMiddleware_MostSpecificFirst()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health/detailed");

                    // Throws if used
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResponseWriter = (c, r) => throw null,
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health/detailed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenOnPort_AcceptsRequest_OnSpecifiedPort()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    app.UseHealthChecks("/health", port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenOnPortWithoutPath_AcceptsRequest_OnSpecifiedPort()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    app.UseHealthChecks(default, port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenOnPort_RejectsRequest_OnOtherPort()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    app.UseHealthChecks("/health", port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5000/health");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CanListenOnPort_MultipleMiddleware()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    // Throws if used
                    app.UseHealthChecks("/health", port: 5001, new HealthCheckOptions()
                    {
                        ResponseWriter = (c, r) => throw null,
                    });

                    app.UseHealthChecks("/health/detailed", port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health/detailed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CanListenOnPort_MultipleMiddleware_DifferentPorts()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    // Throws if used
                    app.UseHealthChecks("/health", port: 5002, new HealthCheckOptions()
                    {
                        ResponseWriter = (c, r) => throw null,
                    });

                    app.UseHealthChecks("/health", port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("http://localhost:5001/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void HealthCheckOptions_HasDefaultMappingWithDefaultResultStatusCodes()
    {
        var options = new HealthCheckOptions();
        Assert.NotNull(options.ResultStatusCodes);
        Assert.Equal(StatusCodes.Status200OK, options.ResultStatusCodes[HealthStatus.Healthy]);
        Assert.Equal(StatusCodes.Status200OK, options.ResultStatusCodes[HealthStatus.Degraded]);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, options.ResultStatusCodes[HealthStatus.Unhealthy]);
    }

    [Fact]
    public void HealthCheckOptions_HasDefaultMappingWhenResettingResultStatusCodes()
    {
        var options = new HealthCheckOptions { ResultStatusCodes = null };
        Assert.NotNull(options.ResultStatusCodes);
        Assert.Equal(StatusCodes.Status200OK, options.ResultStatusCodes[HealthStatus.Healthy]);
        Assert.Equal(StatusCodes.Status200OK, options.ResultStatusCodes[HealthStatus.Degraded]);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, options.ResultStatusCodes[HealthStatus.Unhealthy]);
    }

    [Fact]
    public void HealthCheckOptions_DoesNotThrowWhenProperlyConfiguringResultStatusCodes()
    {
        _ = new HealthCheckOptions
        {
            ResultStatusCodes = new Dictionary<HealthStatus, int>
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503
            }
        };
    }

    [Fact]
    public void HealthCheckOptions_ThrowsWhenAHealthStatusIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new HealthCheckOptions { ResultStatusCodes = new Dictionary<HealthStatus, int>() }
        );
        Assert.Contains($"{nameof(HealthStatus)}.{nameof(HealthStatus.Healthy)}", exception.Message);
        Assert.Contains($"{nameof(HealthStatus)}.{nameof(HealthStatus.Degraded)}", exception.Message);
        Assert.Contains($"{nameof(HealthStatus)}.{nameof(HealthStatus.Unhealthy)}", exception.Message);
    }

    [Fact]
    public void HealthCheckOptions_ThrowsWhenAHealthStatusIsMissing_MessageDoesNotContainDefinedStatus()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new HealthCheckOptions
            {
                ResultStatusCodes = new Dictionary<HealthStatus, int>
                {
                    [HealthStatus.Healthy] = 200
                }
            }
        );
        Assert.DoesNotContain($"{nameof(HealthStatus)}.{nameof(HealthStatus.Healthy)}", exception.Message);
        Assert.Contains($"{nameof(HealthStatus)}.{nameof(HealthStatus.Degraded)}", exception.Message);
        Assert.Contains($"{nameof(HealthStatus)}.{nameof(HealthStatus.Unhealthy)}", exception.Message);
    }
}
