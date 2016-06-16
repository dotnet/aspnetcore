// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Session
{
    public class SessionTests
    {
        [Fact]
        public async Task ReadingEmptySessionDoesNotCreateCookie()
        {
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
                IEnumerable<string> values;
                Assert.False(response.Headers.TryGetValues("Set-Cookie", out values));
            }
        }

        [Fact]
        public async Task SettingAValueCausesTheCookieToBeCreated()
        {
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
                IEnumerable<string> values;
                Assert.True(response.Headers.TryGetValues("Set-Cookie", out values));
                Assert.Equal(1, values.Count());
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
            var builder = new WebHostBuilder()
               .Configure(app =>
               {
                   app.UseSession(new SessionOptions
                   {
                       CookieName = "TestCookie",
                       CookieSecure = cookieSecurePolicy
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();
                IEnumerable<string> values;
                Assert.True(response.Headers.TryGetValues("Set-Cookie", out values));
                Assert.Equal(1, values.Count());
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
            var builder = new WebHostBuilder()
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
                        return context.Response.WriteAsync(value.Value.ToString());
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });

            using (var server = new TestServer(builder))
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
            var builder = new WebHostBuilder()
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
                        return context.Response.WriteAsync(value.Value.ToString());
                    });
                })
                .ConfigureServices(
                services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });

            using (var server = new TestServer(builder))
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
            var builder = new WebHostBuilder()
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
                        return context.Response.WriteAsync(value.Value.ToString());
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.AddSession();
                });

            using (var server = new TestServer(builder))
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
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
            }

            var sessionLogMessages = sink.Writes.OnlyMessagesFromSource<DistributedSession>().ToArray();

            Assert.Equal(2, sessionLogMessages.Length);
            Assert.Contains("started", sessionLogMessages[0].State.ToString());
            Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);
            Assert.Contains("stored", sessionLogMessages[1].State.ToString());
            Assert.Equal(LogLevel.Debug, sessionLogMessages[1].LogLevel);
        }

        [Fact]
        public async Task ExpiredSession_LogsWarning()
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var builder = new WebHostBuilder()
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
                        return context.Response.WriteAsync(value.Value.ToString());
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                    services.AddDistributedMemoryCache();
                    services.AddSession(o => o.IdleTimeout = TimeSpan.FromMilliseconds(30));
                });

            string result;
            using (var server = new TestServer(builder))
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

            var sessionLogMessages = sink.Writes.OnlyMessagesFromSource<DistributedSession>().ToArray();

            Assert.Equal("2", result);
            Assert.Equal(3, sessionLogMessages.Length);
            Assert.Contains("started", sessionLogMessages[0].State.ToString());
            Assert.Contains("stored", sessionLogMessages[1].State.ToString());
            Assert.Contains("expired", sessionLogMessages[2].State.ToString());
            Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);
            Assert.Equal(LogLevel.Debug, sessionLogMessages[1].LogLevel);
            Assert.Equal(LogLevel.Warning, sessionLogMessages[2].LogLevel);
        }

        [Fact]
        public async Task RefreshesSession_WhenSessionData_IsNotModified()
        {
            var clock = new TestClock();
            var builder = new WebHostBuilder()
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
                            responseData = (value == null) ? "No value found in session." : value.ToString();
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
                    services.AddSingleton(typeof(ILoggerFactory), new NullLoggerFactory());
                    services.AddDistributedMemoryCache();
                    services.AddSession(o => o.IdleTimeout = TimeSpan.FromMinutes(20));
                    services.Configure<MemoryCacheOptions>(o => o.Clock = clock);
                });

            using (var server = new TestServer(builder))
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
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        await next();

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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task SessionFeature_IsUnregistered_WhenResponseGoingOut_AndAnUnhandledExcetionIsThrown()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        var exceptionThrown = false;
                        try
                        {
                            await next();
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
            }
        }

        [Fact]
        public async Task SessionKeys_AreCaseSensitive()
        {
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task SessionLogsCacheReadException()
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseSession();
                    app.Run(context =>
                    {
                        byte[] value;
                        Assert.False(context.Session.TryGetValue("key", out value));
                        Assert.Equal(null, value);
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
            }

            var sessionLogMessages = sink.Writes.OnlyMessagesFromSource<DistributedSession>().ToArray();

            Assert.Equal(1, sessionLogMessages.Length);
            Assert.Contains("Session cache read exception", sessionLogMessages[0].State.ToString());
            Assert.Equal(LogLevel.Error, sessionLogMessages[0].LogLevel);
        }

        [Fact]
        public async Task SessionLogsCacheWriteException()
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
            }

            var sessionLogMessages = sink.Writes.OnlyMessagesFromSource<DistributedSession>().ToArray();

            Assert.Equal(1, sessionLogMessages.Length);
            Assert.Contains("Session started", sessionLogMessages[0].State.ToString());
            Assert.Equal(LogLevel.Information, sessionLogMessages[0].LogLevel);

            var sessionMiddlewareLogMessages = sink.Writes.OnlyMessagesFromSource<SessionMiddleware>().ToArray();
            Assert.Equal(1, sessionMiddlewareLogMessages.Length);
            Assert.Contains("Error closing the session.", sessionMiddlewareLogMessages[0].State.ToString());
            Assert.Equal(LogLevel.Error, sessionMiddlewareLogMessages[0].LogLevel);
        }

        [Fact]
        public async Task SessionLogsCacheRefreshException()
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
            }

            var sessionLogMessages = sink.Writes.OnlyMessagesFromSource<SessionMiddleware>().ToArray();

            Assert.Equal(1, sessionLogMessages.Length);
            Assert.Contains("Error closing the session.", sessionLogMessages[0].State.ToString());
            Assert.Equal(LogLevel.Error, sessionLogMessages[0].LogLevel);
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

            public UnreliableCache(IMemoryCache memoryCache)
            {
                _cache = new MemoryDistributedCache(memoryCache);
            }

            public byte[] Get(string key)
            {
                if (DisableGet)
                {
                    throw new InvalidOperationException();
                }
                return _cache.Get(key);
            }
            public Task<byte[]> GetAsync(string key) => _cache.GetAsync(key);
            public void Refresh(string key) => _cache.Refresh(key);
            public Task RefreshAsync(string key)
            {
                if (DisableRefreshAsync)
                {
                    throw new InvalidOperationException();
                }
                return _cache.RefreshAsync(key);
            }
            public void Remove(string key) => _cache.Remove(key);
            public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _cache.Set(key, value, options);
            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                if (DisableSetAsync)
                {
                    throw new InvalidOperationException();
                }
                return  _cache.SetAsync(key, value, options);
            }
        }
    }
}