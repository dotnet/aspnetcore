// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpsPolicy.Tests
{
    public class HstsMiddlewareTests
    {
#region Unit tests
        [Fact]
        public void HstsMiddleware_ArgumentNextIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new HstsMiddleware(next: null, options: new OptionsWrapper<HstsOptions>(new HstsOptions()));
            });
        }

        [Fact]
        public void HstsMiddleware_ArgumentOptionsIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new HstsMiddleware(innerHttpContext => Task.CompletedTask, options: null);
            });
        }

        [Fact]
        public async Task Invoke_SetsHstsHeader()
        {
            var middleware = new HstsMiddleware(innerHttpContext => Task.CompletedTask, new OptionsWrapper<HstsOptions>(new HstsOptions()));

            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("example.com");
            context.Request.Scheme = "https";

            await using (context.Response.Body = new MemoryStream())
            {
                await middleware.Invoke(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                using var streamReader = new StreamReader(context.Response.Body);
                _ = await streamReader.ReadToEndAsync();
            }

            Assert.Equal("max-age=2592000", context.Response.Headers[HeaderNames.StrictTransportSecurity].FirstOrDefault());
        }

        [Theory]
        [InlineData(0, false, false, "max-age=0")]
        [InlineData(-1, false, false, "max-age=-1")]
        [InlineData(0, true, false, "max-age=0; includeSubDomains")]
        [InlineData(50000, false, true, "max-age=50000; preload")]
        [InlineData(0, true, true, "max-age=0; includeSubDomains; preload")]
        [InlineData(50000, true, true, "max-age=50000; includeSubDomains; preload")]
        public async Task Invoke_SetsHeaderCorrectly(int maxAge, bool includeSubDomains, bool preload, string expected)
        {
            var hstsOptions = new HstsOptions
            {
                Preload = preload,
                IncludeSubDomains = includeSubDomains,
                MaxAge = TimeSpan.FromSeconds(maxAge)
            };

            var middleware = new HstsMiddleware(innerHttpContext => Task.CompletedTask, new OptionsWrapper<HstsOptions>(hstsOptions));

            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("example.com");
            context.Request.Scheme = "https";

            await using (context.Response.Body = new MemoryStream())
            {
                await middleware.Invoke(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                using var streamReader = new StreamReader(context.Response.Body);
                _ = await streamReader.ReadToEndAsync();
            }

            Assert.Equal(expected, context.Response.Headers[HeaderNames.StrictTransportSecurity].FirstOrDefault());
        }

        [Theory]
        [InlineData(0, false, false)]
        [InlineData(-1, false, false)]
        [InlineData(0, true, false)]
        [InlineData(50000, false, true)]
        [InlineData(0, true, true)]
        [InlineData(50000, true, true)]
        public async Task Invoke_SchemeIsInsecure_DoesNotSetHstsHeader(int maxAge, bool includeSubDomains, bool preload)
        {
            var hstsOptions = new HstsOptions
            {
                Preload = preload,
                IncludeSubDomains = includeSubDomains,
                MaxAge = TimeSpan.FromSeconds(maxAge)
            };

            var middleware = new HstsMiddleware(innerHttpContext => Task.CompletedTask, new OptionsWrapper<HstsOptions>(hstsOptions));

            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("example.com");
            context.Request.Scheme = "http";

            await using (context.Response.Body = new MemoryStream())
            {
                await middleware.Invoke(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                using var streamReader = new StreamReader(context.Response.Body);
                _ = await streamReader.ReadToEndAsync();
            }

            Assert.Null(context.Response.Headers[HeaderNames.StrictTransportSecurity].FirstOrDefault());
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        public async Task Invoke_HostIsExcluded_DoesNotSetHstsHeader(string hostName)
        {
            var middleware = new HstsMiddleware(innerHttpContext => Task.CompletedTask, new OptionsWrapper<HstsOptions>(new HstsOptions()));

            var context = new DefaultHttpContext();
            context.Request.Host = new HostString(hostName);
            context.Request.Scheme = "https";

            await using (context.Response.Body = new MemoryStream())
            {
                await middleware.Invoke(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                using var streamReader = new StreamReader(context.Response.Body);
                _ = await streamReader.ReadToEndAsync();
            }

            Assert.Null(context.Response.Headers[HeaderNames.StrictTransportSecurity].FirstOrDefault());
        }
#endregion // Unit tests

#region Integration tests
        [Fact]
        public async Task SetOptionsWithDefault_SetsMaxAgeToCorrectValue()
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");

            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("max-age=2592000", response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
        }

        [Theory]
        [InlineData(0, false, false, "max-age=0")]
        [InlineData(-1, false, false, "max-age=-1")]
        [InlineData(0, true, false, "max-age=0; includeSubDomains")]
        [InlineData(50000, false, true, "max-age=50000; preload")]
        [InlineData(0, true, true, "max-age=0; includeSubDomains; preload")]
        [InlineData(50000, true, true, "max-age=50000; includeSubDomains; preload")]
        public async Task SetOptionsThroughConfigure_SetsHeaderCorrectly(int maxAge, bool includeSubDomains, bool preload, string expected)
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.Configure<HstsOptions>(options =>
                        {
                            options.Preload = preload;
                            options.IncludeSubDomains = includeSubDomains;
                            options.MaxAge = TimeSpan.FromSeconds(maxAge);
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
        }

        [Theory]
        [InlineData(0, false, false, "max-age=0")]
        [InlineData(-1, false, false, "max-age=-1")]
        [InlineData(0, true, false, "max-age=0; includeSubDomains")]
        [InlineData(50000, false, true, "max-age=50000; preload")]
        [InlineData(0, true, true, "max-age=0; includeSubDomains; preload")]
        [InlineData(50000, true, true, "max-age=50000; includeSubDomains; preload")]
        public async Task SetOptionsThroughHelper_SetsHeaderCorrectly(int maxAge, bool includeSubDomains, bool preload, string expected)
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddHsts(options =>
                        {
                            options.Preload = preload;
                            options.IncludeSubDomains = includeSubDomains;
                            options.MaxAge = TimeSpan.FromSeconds(maxAge);
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("Localhost")]
        [InlineData("LOCALHOST")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        public async Task DefaultExcludesCommonLocalhostDomains_DoesNotSetHstsHeader(string hostUrl)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<HstsMiddleware>,
                TestSink.EnableWithTypeName<HstsMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(loggerFactory);
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri($"https://{hostUrl}:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            var logMessages = sink.Writes.ToList();

            Assert.Single(logMessages);
            var message = logMessages.Single();
            Assert.Equal(LogLevel.Debug, message.LogLevel);
            Assert.Equal($"The host '{hostUrl}' is excluded. Skipping HSTS header.", message.State.ToString(), ignoreCase: true);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        public async Task AllowLocalhostDomainsIfListIsReset_SetHstsHeader(string hostUrl)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<HstsMiddleware>,
                TestSink.EnableWithTypeName<HstsMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(loggerFactory);

                        services.AddHsts(options =>
                        {
                            options.ExcludedHosts.Clear();
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri($"https://{hostUrl}:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(response.Headers);

            var logMessages = sink.Writes.ToList();

            Assert.Single(logMessages);
            var message = logMessages.Single();
            Assert.Equal(LogLevel.Trace, message.LogLevel);
            Assert.Equal("Adding HSTS header to response.", message.State.ToString());
        }

        [Theory]
        [InlineData("example.com")]
        [InlineData("Example.com")]
        [InlineData("EXAMPLE.COM")]
        public async Task AddExcludedDomains_DoesNotAddHstsHeader(string hostUrl)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<HstsMiddleware>,
                TestSink.EnableWithTypeName<HstsMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(loggerFactory);

                        services.AddHsts(options =>
                        {
                            options.ExcludedHosts.Add(hostUrl);
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri($"https://{hostUrl}:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            var logMessages = sink.Writes.ToList();

            Assert.Single(logMessages);
            var message = logMessages.Single();
            Assert.Equal(LogLevel.Debug, message.LogLevel);
            Assert.Equal($"The host '{hostUrl}' is excluded. Skipping HSTS header.", message.State.ToString(), ignoreCase: true);
        }

        [Fact]
        public async Task WhenRequestIsInsecure_DoesNotAddHstsHeader()
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<HstsMiddleware>,
                TestSink.EnableWithTypeName<HstsMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(loggerFactory);
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://example.com:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            var logMessages = sink.Writes.ToList();

            Assert.Single(logMessages);
            var message = logMessages.Single();
            Assert.Equal(LogLevel.Debug, message.LogLevel);
            Assert.Equal("The request is insecure. Skipping HSTS header.", message.State.ToString());
        }

        [Fact]
        public async Task WhenRequestIsSecure_AddsHstsHeader()
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<HstsMiddleware>,
                TestSink.EnableWithTypeName<HstsMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(loggerFactory);
                    })
                    .Configure(app =>
                    {
                        app.UseHsts();
                        app.Run(context =>
                        {
                            return context.Response.WriteAsync("Hello world");
                        });
                    });
                }).Build();

            await host.StartAsync();
            var server = host.GetTestServer();
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(response.Headers, x => x.Key == HeaderNames.StrictTransportSecurity);

            var logMessages = sink.Writes.ToList();

            Assert.Single(logMessages);
            var message = logMessages.Single();
            Assert.Equal(LogLevel.Trace, message.LogLevel);
            Assert.Equal("Adding HSTS header to response.", message.State.ToString());
        }
#endregion // Integration tests
    }
}
