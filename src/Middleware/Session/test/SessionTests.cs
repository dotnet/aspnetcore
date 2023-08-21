// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Session;

public class SessionTests
{
    [Fact]
    public async Task ReadingEmptySessionDoesNotCreateCookie()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();

                    app.Run(context =>
                    {
                        Assert.Null(context.Session.GetString("NotFound"));
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.False(response.Headers.TryGetValues("Set-Cookie", out var _));
        }
    }

    [Fact]
    public async Task SettingAValueCausesTheCookieToBeCreated()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        Assert.Null(context.Session.GetString("Key"));
                        context.Session.SetString("Key", "Value");
                        Assert.Equal("Value", context.Session.GetString("Key"));
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
            Assert.Single(values);
            Assert.True(!string.IsNullOrWhiteSpace(values.First()));
        }
    }

    [Theory]
    [InlineData(CookieSecurePolicy.Always, "http://example.com/testpath", true)]
    [InlineData(CookieSecurePolicy.Always, "https://example.com/testpath", true)]
    [InlineData(CookieSecurePolicy.None, "http://example.com/testpath", false)]
    [InlineData(CookieSecurePolicy.None, "https://example.com/testpath", false)]
    [InlineData(CookieSecurePolicy.SameAsRequest, "http://example.com/testpath", false)]
    [InlineData(CookieSecurePolicy.SameAsRequest, "https://example.com/testpath", true)]
    public async Task SecureSessionBasedOnHttpsAndSecurePolicy(
        CookieSecurePolicy cookieSecurePolicy,
        string requestUri,
        bool shouldBeSecureOnly)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession(new SessionOptions
                    {
                        Cookie =
                        {
                                Name = "TestCookie",
                                SecurePolicy = cookieSecurePolicy
                        }
                    });
                    app.Run(context =>
                    {
                        Assert.Null(context.Session.GetString("Key"));
                        context.Session.SetString("Key", "Value");
                        Assert.Equal("Value", context.Session.GetString("Key"));
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
            Assert.Single(values);
            if (shouldBeSecureOnly)
            {
                Assert.Contains("; secure", values.First());
            }
            else
            {
                Assert.DoesNotContain("; secure", values.First());
            }
        }
    }

    [Fact]
    public async Task SessionCanBeAccessedOnTheNextRequest()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        int? value = context.Session.GetInt32("Key");
                        if (context.Request.Path == new PathString("/first"))
                        {
                            Assert.False(value.HasValue);
                            value = 0;
                        }
                        Assert.True(value.HasValue);
                        context.Session.SetInt32("Key", value.Value + 1);
                        return context.Response.WriteAsync(value.Value.ToString(CultureInfo.InvariantCulture));
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("first");
            response.EnsureSuccessStatusCode();
            Assert.Equal("0", await response.Content.ReadAsStringAsync());

            client = server.CreateClient();
            var cookie = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()).First();
            client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
            Assert.Equal("1", await client.GetStringAsync("/"));
            Assert.Equal("2", await client.GetStringAsync("/"));
            Assert.Equal("3", await client.GetStringAsync("/"));
        }
    }

    [Fact]
    public async Task RemovedItemCannotBeAccessedAgain()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        int? value = context.Session.GetInt32("Key");
                        if (context.Request.Path == new PathString("/first"))
                        {
                            Assert.False(value.HasValue);
                            value = 0;
                            context.Session.SetInt32("Key", 1);
                        }
                        else if (context.Request.Path == new PathString("/second"))
                        {
                            Assert.True(value.HasValue);
                            Assert.Equal(1, value);
                            context.Session.Remove("Key");
                        }
                        else if (context.Request.Path == new PathString("/third"))
                        {
                            Assert.False(value.HasValue);
                            value = 2;
                        }
                        return context.Response.WriteAsync(value.Value.ToString(CultureInfo.InvariantCulture));
                    });
                })
                .ConfigureServices(
                services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("first");
            response.EnsureSuccessStatusCode();
            Assert.Equal("0", await response.Content.ReadAsStringAsync());

            client = server.CreateClient();
            var cookie = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()).First();
            client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
            Assert.Equal("1", await client.GetStringAsync("/second"));
            Assert.Equal("2", await client.GetStringAsync("/third"));
        }
    }

    [Fact]
    public async Task ClearedItemsCannotBeAccessedAgain()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        int? value = context.Session.GetInt32("Key");
                        if (context.Request.Path == new PathString("/first"))
                        {
                            Assert.False(value.HasValue);
                            value = 0;
                            context.Session.SetInt32("Key", 1);
                        }
                        else if (context.Request.Path == new PathString("/second"))
                        {
                            Assert.True(value.HasValue);
                            Assert.Equal(1, value);
                            context.Session.Clear();
                        }
                        else if (context.Request.Path == new PathString("/third"))
                        {
                            Assert.False(value.HasValue);
                            value = 2;
                        }
                        return context.Response.WriteAsync(value.Value.ToString(CultureInfo.InvariantCulture));
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("first");
            response.EnsureSuccessStatusCode();
            Assert.Equal("0", await response.Content.ReadAsStringAsync());

            client = server.CreateClient();
            var cookie = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()).First();
            client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
            Assert.Equal("1", await client.GetStringAsync("/second"));
            Assert.Equal("2", await client.GetStringAsync("/third"));
        }
    }

    [Fact]
    public async Task SessionStart_LogsInformation()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DistributedSession>,
            TestSink.EnableWithTypeName<DistributedSession>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        context.Session.SetString("Key", "Value");
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        var sessionLogMessages = sink.Writes.ToList();

        Assert.Equal(2, sessionLogMessages.Count);
        Assert.Contains("started", sessionLogMessages[0].State.ToString());
        Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);
        Assert.Contains("stored", sessionLogMessages[1].State.ToString());
        Assert.Equal(LogLevel.Debug, sessionLogMessages[1].LogLevel);
    }

    [Fact]
    public async Task ExpiredSession_LogsInfo()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DistributedSession>,
            TestSink.EnableWithTypeName<DistributedSession>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        int? value = context.Session.GetInt32("Key");
                        if (context.Request.Path == new PathString("/first"))
                        {
                            Assert.False(value.HasValue);
                            value = 1;
                            context.Session.SetInt32("Key", 1);
                        }
                        else if (context.Request.Path == new PathString("/second"))
                        {
                            Assert.False(value.HasValue);
                            value = 2;
                        }
                        return context.Response.WriteAsync(value.Value.ToString(CultureInfo.InvariantCulture));
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddDistributedMemoryCache();
                    services.AddSession(o => o.IdleTimeout = TimeSpan.FromMilliseconds(30));
                });
            }).Build();

        await host.StartAsync();

        string result;
        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("first");
            response.EnsureSuccessStatusCode();

            client = server.CreateClient();
            var cookie = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()).First();
            client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
            Thread.Sleep(50);
            result = await client.GetStringAsync("/second");
        }

        var sessionLogMessages = sink.Writes.ToList();

        Assert.Equal("2", result);
        Assert.Equal(3, sessionLogMessages.Count);
        Assert.Contains("started", sessionLogMessages[0].State.ToString());
        Assert.Contains("stored", sessionLogMessages[1].State.ToString());
        Assert.Contains("expired", sessionLogMessages[2].State.ToString());
        Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);
        Assert.Equal(LogLevel.Debug, sessionLogMessages[1].LogLevel);
        Assert.Equal(LogLevel.Information, sessionLogMessages[2].LogLevel);
    }

    [Fact]
    public async Task RefreshesSession_WhenSessionData_IsNotModified()
    {
        var clock = new TestClock();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        string responseData = string.Empty;
                        if (context.Request.Path == new PathString("/AddDataToSession"))
                        {
                            context.Session.SetInt32("Key", 10);
                            responseData = "added data to session";
                        }
                        else if (context.Request.Path == new PathString("/AccessSessionData"))
                        {
                            var value = context.Session.GetInt32("Key");
                            responseData = (value == null) ? "No value found in session." : value.Value.ToString(CultureInfo.InvariantCulture);
                        }
                        else if (context.Request.Path == new PathString("/DoNotAccessSessionData"))
                        {
                            responseData = "did not access session data";
                        }

                        return context.Response.WriteAsync(responseData);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), NullLoggerFactory.Instance);
                    services.AddDistributedMemoryCache();
                    services.AddSession(o => o.IdleTimeout = TimeSpan.FromMinutes(20));
                    services.Configure<MemoryCacheOptions>(o => o.Clock = clock);
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("AddDataToSession");
            response.EnsureSuccessStatusCode();

            client = server.CreateClient();
            var cookie = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()).First();
            client.DefaultRequestHeaders.Add(
                "Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());

            for (var i = 0; i < 5; i++)
            {
                clock.Add(TimeSpan.FromMinutes(10));
                await client.GetStringAsync("/DoNotAccessSessionData");
            }

            var data = await client.GetStringAsync("/AccessSessionData");
            Assert.Equal("10", data);
        }
    }

    [Fact]
    public async Task SessionFeature_IsUnregistered_WhenResponseGoingOut()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        await next(httpContext);

                        Assert.Null(httpContext.Features.Get<ISessionFeature>());
                    });

                    app.UseSession();

                    app.Run(context =>
                    {
                        context.Session.SetString("key", "value");
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task SessionFeature_IsUnregistered_WhenResponseGoingOut_AndAnUnhandledExcetionIsThrown()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        var exceptionThrown = false;
                        try
                        {
                            await next(httpContext);
                        }
                        catch
                        {
                            exceptionThrown = true;
                        }

                        Assert.True(exceptionThrown);
                        Assert.Null(httpContext.Features.Get<ISessionFeature>());
                    });

                    app.UseSession();

                    app.Run(context =>
                    {
                        throw new InvalidOperationException("An error occurred.");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
        }
    }

    [Fact]
    public async Task SessionKeys_AreCaseSensitive()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        context.Session.SetString("KEY", "VALUE");
                        context.Session.SetString("key", "value");
                        Assert.Equal("VALUE", context.Session.GetString("KEY"));
                        Assert.Equal("value", context.Session.GetString("key"));
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task SessionLogsCacheReadException()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DistributedSession>,
            TestSink.EnableWithTypeName<DistributedSession>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        Assert.False(context.Session.TryGetValue("key", out var value));
                        Assert.Null(value);
                        Assert.Equal(string.Empty, context.Session.Id);
                        Assert.False(context.Session.Keys.Any());
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DisableGet = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        Assert.NotEmpty(sink.Writes);
        var message = sink.Writes.First();
        Assert.Contains("Session cache read exception", message.State.ToString());
        Assert.Equal(LogLevel.Error, message.LogLevel);
    }

    [Fact]
    public async Task SessionLogsCacheLoadAsyncException()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DistributedSession>,
            TestSink.EnableWithTypeName<DistributedSession>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(async context =>
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Session.LoadAsync());
                        Assert.False(context.Session.IsAvailable);
                        Assert.Equal(string.Empty, context.Session.Id);
                        Assert.False(context.Session.Keys.Any());
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DisableGet = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        Assert.NotEmpty(sink.Writes);
        var message = sink.Writes.First();
        Assert.Contains("Session cache read exception", message.State.ToString());
        Assert.Equal(LogLevel.Error, message.LogLevel);
    }

    [Fact]
    public async Task SessionLogsCacheLoadAsyncTimeoutException()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DistributedSession>,
            TestSink.EnableWithTypeName<DistributedSession>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession(new SessionOptions()
                    {
                        IOTimeout = TimeSpan.FromSeconds(0.5)
                    });
                    app.Run(async context =>
                    {
                        await Assert.ThrowsAsync<OperationCanceledException>(() => context.Session.LoadAsync());
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DelayGetAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        var message = Assert.Single(sink.Writes);
        Assert.Contains("Loading the session timed out.", message.State.ToString());
        Assert.Equal(LogLevel.Warning, message.LogLevel);
    }

    [Fact]
    public async Task SessionLoadAsyncCanceledException()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DistributedSession>,
            TestSink.EnableWithTypeName<DistributedSession>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(async context =>
                    {
                        var cts = new CancellationTokenSource();
                        var token = cts.Token;
                        cts.Cancel();
                        await Assert.ThrowsAsync<OperationCanceledException>(() => context.Session.LoadAsync(token));
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DelayGetAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        Assert.Empty(sink.Writes);
    }

    [Fact]
    public async Task SessionLogsCacheCommitException()
    {
        var sink = new TestSink(
            writeContext =>
            {
                return writeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || writeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            },
            beginScopeContext =>
            {
                return beginScopeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || beginScopeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            });
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        context.Session.SetInt32("key", 0);
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DisableSetAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        var sessionLogMessage = sink.Writes.Where(message => message.LoggerName.Equals(typeof(DistributedSession).FullName, StringComparison.Ordinal)).Single();

        Assert.Contains("Session started", sessionLogMessage.State.ToString());
        Assert.Equal(LogLevel.Information, sessionLogMessage.LogLevel);

        var sessionMiddlewareLogMessage = sink.Writes.Where(message => message.LoggerName.Equals(typeof(SessionMiddleware).FullName, StringComparison.Ordinal)).Single();

        Assert.Contains("Error closing the session.", sessionMiddlewareLogMessage.State.ToString());
        Assert.Equal(LogLevel.Error, sessionMiddlewareLogMessage.LogLevel);
    }

    [Fact]
    public async Task SessionLogsCacheCommitTimeoutException()
    {
        var sink = new TestSink(
            writeContext =>
            {
                return writeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || writeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            },
            beginScopeContext =>
            {
                return beginScopeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || beginScopeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            });
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession(new SessionOptions()
                    {
                        IOTimeout = TimeSpan.FromSeconds(0.5)
                    });
                    app.Run(context =>
                    {
                        context.Session.SetInt32("key", 0);
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DelaySetAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        var sessionLogMessages = sink.Writes.Where(message => message.LoggerName.Equals(typeof(DistributedSession).FullName, StringComparison.Ordinal)).ToList();

        Assert.Contains("Session started", sessionLogMessages[0].State.ToString());
        Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);

        Assert.Contains("Committing the session timed out.", sessionLogMessages[1].State.ToString());
        Assert.Equal(LogLevel.Warning, sessionLogMessages[1].LogLevel);

        var sessionMiddlewareLogs = sink.Writes.Where(message => message.LoggerName.Equals(typeof(SessionMiddleware).FullName, StringComparison.Ordinal)).ToList();

        Assert.Contains("Committing the session was canceled.", sessionMiddlewareLogs[0].State.ToString());
        Assert.Equal(LogLevel.Information, sessionMiddlewareLogs[0].LogLevel);
    }

    [Fact]
    public async Task SessionLogsCacheCommitCanceledException()
    {
        var sink = new TestSink(
            writeContext =>
            {
                return writeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || writeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            },
            beginScopeContext =>
            {
                return beginScopeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || beginScopeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            });
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(async context =>
                    {
                        context.Session.SetInt32("key", 0);
                        var cts = new CancellationTokenSource();
                        var token = cts.Token;
                        cts.Cancel();
                        await Assert.ThrowsAsync<OperationCanceledException>(() => context.Session.CommitAsync(token));
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DelaySetAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        // The session is automatically committed on unwind even after the manual commit was canceled.
        var sessionLogMessages = sink.Writes.Where(message => message.LoggerName.Equals(typeof(DistributedSession).FullName, StringComparison.Ordinal)).ToList();

        Assert.Contains("Session started", sessionLogMessages[0].State.ToString());
        Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);

        Assert.Contains("Session stored", sessionLogMessages[1].State.ToString());
        Assert.Equal(LogLevel.Debug, sessionLogMessages[1].LogLevel);

        Assert.Empty(sink.Writes.Where(message => message.LoggerName.Equals(typeof(SessionMiddleware).FullName, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task RequestAbortedIgnored()
    {
        var sink = new TestSink(
            writeContext =>
            {
                return writeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || writeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            },
            beginScopeContext =>
            {
                return beginScopeContext.LoggerName.Equals(typeof(SessionMiddleware).FullName)
                    || beginScopeContext.LoggerName.Equals(typeof(DistributedSession).FullName);
            });
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        context.Session.SetInt32("key", 0);
                        var cts = new CancellationTokenSource();
                        var token = cts.Token;
                        cts.Cancel();
                        context.RequestAborted = token;
                        return Task.CompletedTask;
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DelaySetAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        var sessionLogMessages = sink.Writes.Where(message => message.LoggerName.Equals(typeof(DistributedSession).FullName, StringComparison.Ordinal)).ToList();

        Assert.Contains("Session started", sessionLogMessages[0].State.ToString());
        Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);

        Assert.Contains("Session stored", sessionLogMessages[1].State.ToString());
        Assert.Equal(LogLevel.Debug, sessionLogMessages[1].LogLevel);

        Assert.Empty(sink.Writes.Where(message => message.LoggerName.Equals(typeof(SessionMiddleware).FullName, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SessionLogsCacheRefreshException()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<SessionMiddleware>,
            TestSink.EnableWithTypeName<SessionMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        // The middleware calls context.Session.CommitAsync() once per request
                        return Task.FromResult(0);
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddSingleton<IDistributedCache>(new UnreliableCache(new MemoryCache(new MemoryCacheOptions()))
                    {
                        DisableRefreshAsync = true
                    });
                    services.AddSession();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
        }

        var message = Assert.Single(sink.Writes);
        Assert.Contains("Error closing the session.", message.State.ToString());
        Assert.Equal(LogLevel.Error, message.LogLevel);
    }

    private class TestClock : ISystemClock
    {
        public TestClock()
        {
            UtcNow = new DateTimeOffset(2013, 1, 1, 1, 0, 0, TimeSpan.Zero);
        }

        public DateTimeOffset UtcNow { get; private set; }

        public void Add(TimeSpan timespan)
        {
            UtcNow = UtcNow.Add(timespan);
        }
    }

    private class UnreliableCache : IDistributedCache
    {
        private readonly MemoryDistributedCache _cache;

        public bool DisableGet { get; set; }
        public bool DisableSetAsync { get; set; }
        public bool DisableRefreshAsync { get; set; }
        public bool DelayGetAsync { get; set; }
        public bool DelaySetAsync { get; set; }
        public bool DelayRefreshAsync { get; set; }

        public UnreliableCache(IMemoryCache memoryCache)
        {
            _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        }

        public byte[] Get(string key)
        {
            if (DisableGet)
            {
                throw new InvalidOperationException();
            }
            return _cache.Get(key);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (DisableGet)
            {
                throw new InvalidOperationException();
            }
            if (DelayGetAsync)
            {
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                token.ThrowIfCancellationRequested();
            }
            return _cache.GetAsync(key, token);
        }

        public void Refresh(string key) => _cache.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (DisableRefreshAsync)
            {
                throw new InvalidOperationException();
            }
            if (DelayRefreshAsync)
            {
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                token.ThrowIfCancellationRequested();
            }
            return _cache.RefreshAsync(key);
        }

        public void Remove(string key) => _cache.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) => _cache.RemoveAsync(key);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _cache.Set(key, value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (DisableSetAsync)
            {
                throw new InvalidOperationException();
            }
            if (DelaySetAsync)
            {
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                token.ThrowIfCancellationRequested();
            }
            return _cache.SetAsync(key, value, options);
        }
    }
}
