// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Session;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Session
{
    public class SessionTests
    {
        [Fact]
        public async Task ReadingEmptySessionDoesNotCreateCookie()
        {
            using (var server = TestServer.Create(app =>
            {
                app.UseSession();

                app.Run(context =>
                {
                    Assert.Null(context.Session.GetString("NotFound"));
                    return Task.FromResult(0);
                });
            },
            services =>
            {
                services.AddCaching();
                services.AddSession();
            }))
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
            using (var server = TestServer.Create(app =>
            {
                app.UseSession();
                app.Run(context =>
                {
                    Assert.Null(context.Session.GetString("Key"));
                    context.Session.SetString("Key", "Value");
                    Assert.Equal("Value", context.Session.GetString("Key"));
                    return Task.FromResult(0);
                });
            },
            services =>
            {
                services.AddCaching();
                services.AddSession();
            }))
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

        [Fact]
        public async Task SessionCanBeAccessedOnTheNextRequest()
        {
            using (var server = TestServer.Create(app =>
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
            },
            services =>
            {
                services.AddCaching();
                services.AddSession();
            }))
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
            using (var server = TestServer.Create(app =>
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
            },
            services =>
            {
                services.AddCaching();
                services.AddSession();
            }))
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
            using (var server = TestServer.Create(app =>
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
            },
            services =>
            {
                services.AddCaching();
                services.AddSession();
            }))
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
            using (var server = TestServer.Create(app =>
            {
                app.UseSession();
                app.Run(context =>
                {
                    context.Session.SetString("Key", "Value");
                    return Task.FromResult(0);
                });
            },
            services =>
            {
                services.AddInstance(typeof(ILoggerFactory), loggerFactory);
                services.AddCaching();
                services.AddSession();
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                response.EnsureSuccessStatusCode();
                Assert.Single(sink.Writes);
                Assert.Contains("started", sink.Writes[0].State.ToString());
                Assert.Equal(LogLevel.Information, sink.Writes[0].LogLevel);
            }
        }

        [Fact]
        public async Task ExpiredSession_LogsWarning()
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            using (var server = TestServer.Create(app =>
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
            },
            services =>
            {
                services.AddInstance(typeof(ILoggerFactory), loggerFactory);
                services.AddCaching();
                services.AddSession();
                services.ConfigureSession(o => o.IdleTimeout = TimeSpan.FromMilliseconds(30));
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("first");
                response.EnsureSuccessStatusCode();

                client = server.CreateClient();
                var cookie = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()).First();
                client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                Thread.Sleep(50);
                Assert.Equal("2", await client.GetStringAsync("/second"));
                Assert.Equal(2, sink.Writes.Count);
                Assert.Contains("started", sink.Writes[0].State.ToString());
                Assert.Contains("expired", sink.Writes[1].State.ToString());
                Assert.Equal(LogLevel.Information, sink.Writes[0].LogLevel);
                Assert.Equal(LogLevel.Warning, sink.Writes[1].LogLevel);
            }
        }
    }
}