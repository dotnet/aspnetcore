// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingTests
    {
        [Fact]
        public async void ServesCachedContentIfAvailable()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                foreach (var header in initialResponse.Headers)
                {
                    Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
                }
                Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesFreshContentIfNotAvailable()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("/different");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesCachedContentIfVaryByMatches()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                context.Response.Headers[HeaderNames.Vary] = HeaderNames.From;
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.From = "user@example.com";
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                foreach (var header in initialResponse.Headers)
                {
                    Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
                }
                Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesFreshContentIfRequestRequirementsNotMet()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(0)
                };
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesFreshContentIfVaryByMismatches()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                context.Response.Headers[HeaderNames.Vary] = HeaderNames.From;
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.From = "user@example.com";
                var initialResponse = await client.GetAsync("");
                client.DefaultRequestHeaders.From = "user2@example.com";
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void Serves504IfOnlyIfCachedHeaderIsSpecified()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                {
                    OnlyIfCached = true
                };
                var subsequentResponse = await client.GetAsync("/different");

                initialResponse.EnsureSuccessStatusCode();
                Assert.Equal(System.Net.HttpStatusCode.GatewayTimeout, subsequentResponse.StatusCode);
            }
        }

        [Fact]
        public async void ServesCachedContentWithoutSetCookie()
        {
            var builder = CreateBuilderWithResponseCaching(async (context) =>
            {
                var uniqueId = Guid.NewGuid().ToString();
                var headers = context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                headers.Date = DateTimeOffset.UtcNow;
                headers.Headers["X-Value"] = uniqueId;
                headers.Headers[HeaderNames.SetCookie] = "cookieName=cookieValue";
                await context.Response.WriteAsync(uniqueId);
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                foreach (var header in initialResponse.Headers)
                {
                    if (!string.Equals(HeaderNames.SetCookie, header.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
                    }
                }
                Assert.True(initialResponse.Headers.Contains(HeaderNames.SetCookie));
                Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.False(subsequentResponse.Headers.Contains(HeaderNames.SetCookie));
                Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesCachedContentIfIHttpSendFileFeatureNotUsed()
        {
            var builder = CreateBuilderWithResponseCaching(
                app =>
                {
                    app.Use(async (context, next) =>
                    {
                        context.Features.Set<IHttpSendFileFeature>(new DummySendFileFeature());
                        await next.Invoke();
                    });
                },
                async (context) =>
                {
                    var uniqueId = Guid.NewGuid().ToString();
                    var headers = context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10)
                    };
                    headers.Date = DateTimeOffset.UtcNow;
                    headers.Headers["X-Value"] = uniqueId;
                    await context.Response.WriteAsync(uniqueId);
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                foreach (var header in initialResponse.Headers)
                {
                    Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
                }
                Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesFreshContentIfIHttpSendFileFeatureUsed()
        {
            var builder = CreateBuilderWithResponseCaching(
                app =>
                {
                    app.Use(async (context, next) =>
                    {
                        context.Features.Set<IHttpSendFileFeature>(new DummySendFileFeature());
                        await next.Invoke();
                    });
                },
                async (context) =>
                {
                    var uniqueId = Guid.NewGuid().ToString();
                    var headers = context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10)
                    };
                    headers.Date = DateTimeOffset.UtcNow;
                    headers.Headers["X-Value"] = uniqueId;
                    await context.Features.Get<IHttpSendFileFeature>().SendFileAsync("dummy", 0, 0, CancellationToken.None);
                    await context.Response.WriteAsync(uniqueId);
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesCachedContentIfSubsequentRequestContainsNoStore()
        {
            var builder = CreateBuilderWithResponseCaching(
                async (context) =>
                {
                    var uniqueId = Guid.NewGuid().ToString();
                    var headers = context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10)
                    };
                    headers.Date = DateTimeOffset.UtcNow;
                    headers.Headers["X-Value"] = uniqueId;
                    await context.Response.WriteAsync(uniqueId);
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                {
                    NoStore = true
                };
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                foreach (var header in initialResponse.Headers)
                {
                    Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
                }
                Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ServesFreshContentIfInitialRequestContainsNoStore()
        {
            var builder = CreateBuilderWithResponseCaching(
                async (context) =>
                {
                    var uniqueId = Guid.NewGuid().ToString();
                    var headers = context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10)
                    };
                    headers.Date = DateTimeOffset.UtcNow;
                    headers.Headers["X-Value"] = uniqueId;
                    await context.Response.WriteAsync(uniqueId);
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                {
                    NoStore = true
                };
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));
                Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

        private static IWebHostBuilder CreateBuilderWithResponseCaching(RequestDelegate requestDelegate) =>
            CreateBuilderWithResponseCaching(app => { }, requestDelegate);

        private static IWebHostBuilder CreateBuilderWithResponseCaching(Action<IApplicationBuilder> configureDelegate, RequestDelegate requestDelegate)
        {
            return new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDistributedResponseCache();
                })
                .Configure(app =>
                {
                    configureDelegate(app);
                    app.UseResponseCaching();
                    app.Run(requestDelegate);
                });
        }

        private class DummySendFileFeature : IHttpSendFileFeature
        {
            public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
            {
                return Task.FromResult(0);
            }
        }
    }
}
