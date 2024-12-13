// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.HttpLogging;

public class HttpLoggingMiddlewareTests : LoggedTest
{
    public static TheoryData<string> BodyData
    {
        get
        {
            var variations = new TheoryData<string>();
            variations.Add("Hello World");
            variations.Add(new string('a', 4097));
            variations.Add(new string('b', 10000));
            variations.Add(new string('あ', 10000));
            return variations;
        }
    }

    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(
            null,
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>(),
            Array.Empty<IHttpLoggingInterceptor>(),
            ObjectPool.Create<HttpLoggingInterceptorContext>(),
            TimeProvider.System));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            null,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>(),
            Array.Empty<IHttpLoggingInterceptor>(),
            ObjectPool.Create<HttpLoggingInterceptorContext>(),
            TimeProvider.System));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            CreateOptionsAccessor(),
            null,
            Array.Empty<IHttpLoggingInterceptor>(),
            ObjectPool.Create<HttpLoggingInterceptorContext>(),
            TimeProvider.System));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>(),
            null,
            ObjectPool.Create<HttpLoggingInterceptorContext>(),
            TimeProvider.System));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>(),
            Array.Empty<IHttpLoggingInterceptor>(),
            null,
            TimeProvider.System));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>(),
            Array.Empty<IHttpLoggingInterceptor>(),
            ObjectPool.Create<HttpLoggingInterceptorContext>(),
            null));
    }

    [Fact]
    public async Task NoopWhenLoggingDisabled()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.None;

        var middleware = CreateMiddleware(options: options);

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);

        Assert.Empty(TestSink.Writes);
    }

    [Fact]
    public async Task DefaultRequestInfoOnlyHeadersAndRequestInfo()
    {
        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            });

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task RequestLogsAllRequestInfo()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Request;
        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            },
            options);

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task RequestPropertiesLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestProperties;
        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            },
            options);

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task RequestHeadersLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestHeaders;
        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            },
            options);

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("QueryString: ?foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task UnknownRequestHeadersRedacted()
    {
        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            });

        var httpContext = new DefaultHttpContext();

        httpContext.Request.Headers["foo"] = "bar";

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("foo: [Redacted]"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("foo: bar"));
    }

    [Fact]
    public async Task CanConfigureRequestAllowList()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.RequestHeaders.Clear();
        options.CurrentValue.RequestHeaders.Add("foo");
        var middleware = CreateMiddleware(options: options);

        var httpContext = new DefaultHttpContext();

        // Header on the default allow list.
        httpContext.Request.Headers["Connection"] = "keep-alive";

        httpContext.Request.Headers["foo"] = "bar";

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("foo: bar"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("foo: [Redacted]"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: [Redacted]"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
    }

    [Fact]
    public async Task LogsMessageIfNotConsumed()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(options: options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: [Not consumed by app]"));
    }

    [Theory]
    [MemberData(nameof(BodyData))]
    public async Task RequestBodyReadingWorks(string expected)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Theory]
    [MemberData(nameof(BodyData))]
    public async Task RequestBodyCopyToWorks(string expected)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var ms = new MemoryStream();
                c.Request.Body.CopyTo(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                var body = await sr.ReadToEndAsync();
                Assert.Equal(expected, body);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Theory]
    [MemberData(nameof(BodyData))]
    public async Task RequestBodyCopyToAsyncWorks(string expected)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var ms = new MemoryStream();
                await c.Request.Body.CopyToAsync(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                var body = await sr.ReadToEndAsync();
                Assert.Equal(expected, body);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task RequestBodyReadingLimitLongCharactersWorks()
    {
        var input = string.Concat(new string('あ', 5));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;
        options.CurrentValue.RequestBodyLogLimit = 4;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var count = 0;
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                    count += res;
                }

                Assert.Equal(15, count);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, options.CurrentValue.RequestBodyLogLimit / 3) + "[Truncated by RequestBodyLogLimit]";

        Assert.Contains(TestSink.Writes, w => w.Message.Equals("RequestBody: " + expected));
    }

    [Fact]
    public async Task RequestBodyReadingLimitWorks()
    {
        var input = string.Concat(new string('a', 60000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var count = 0;
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                    count += res;
                }

                Assert.Equal(63000, count);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, options.CurrentValue.RequestBodyLogLimit) + "[Truncated by RequestBodyLogLimit]";

        Assert.Contains(TestSink.Writes, w => w.Message.Equals("RequestBody: " + expected));
    }

    [Fact]
    public async Task PartialReadBodyStillLogs()
    {
        var input = string.Concat(new string('a', 60000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var res = await c.Request.Body.ReadAsync(arr);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, 4096) + "[Only partially consumed by app]";

        Assert.Contains(TestSink.Writes, w => w.Message.Equals("RequestBody: " + expected));
    }

    [Fact]
    public async Task ZeroByteReadStillLogsRequestBody()
    {
        var input = string.Concat(new string('a', 60000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                _ = await c.Request.Body.ReadAsync(new byte[0]);
                var res = await c.Request.Body.ReadAsync(arr);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, 4096) + "[Only partially consumed by app]";

        Assert.Contains(TestSink.Writes, w => w.Message.Equals("RequestBody: " + expected));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    [InlineData("application/entity+json")]
    [InlineData("application/entity+xml")]
    public async Task VerifyDefaultMediaTypeHeaders(string contentType)
    {
        // media headers that should work.
        var expected = new string('a', 1000);
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                }
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = contentType;
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Theory]
    [InlineData("application/invalid")]
    [InlineData("application/invalid; charset=utf-8")]
    [InlineData("multipart/form-data")]
    public async Task RejectedContentTypes(string contentType)
    {
        // media headers that should work.
        var expected = new string('a', 1000);
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var count = 0;

                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                    count += res;
                }

                Assert.Equal(1000, count);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = contentType;
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains(expected));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Unrecognized Content-Type for request body."));
    }

    [Fact]
    public async Task DifferentEncodingsWork()
    {
        var encoding = Encoding.Unicode;
        var expected = new string('a', 1000);
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;
        options.CurrentValue.MediaTypeOptions.Clear();
        options.CurrentValue.MediaTypeOptions.AddText("text/plain", encoding);

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var count = 0;
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                    count += res;
                }

                Assert.Equal(2000, count);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(encoding.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task CharsetHonoredIfSupported()
    {
        var encoding = Encoding.Unicode;
        var expected = new string('a', 1000);
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;
        options.CurrentValue.MediaTypeOptions.Clear();
        options.CurrentValue.MediaTypeOptions.AddText("text/plain", encoding);

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var count = 0;
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                    count += res;
                }

                Assert.Equal(1000, count);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain; charset=" + Encoding.ASCII.WebName;
        httpContext.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody:"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task CharsetNotHonoredIfNotSupported()
    {
        var encoding = Encoding.Unicode;
        var expected = new string('a', 1000);
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;
        options.CurrentValue.MediaTypeOptions.Clear();
        options.CurrentValue.MediaTypeOptions.AddText("text/plain", encoding);

        var middleware = CreateMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var count = 0;
                while (true)
                {
                    var res = await c.Request.Body.ReadAsync(arr);
                    if (res == 0)
                    {
                        break;
                    }
                    count += res;
                }

                Assert.Equal(4000, count);
            },
            options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain; charset=" + Encoding.UTF32.WebName;
        httpContext.Request.Body = new MemoryStream(Encoding.UTF32.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("RequestBody:"));
    }

    [Fact]
    public async Task RequestInterceptorCanDisableRequestAndResponseLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;

        var middleware = CreateMiddleware(RequestResponseApp, options, new FakeInterceptor(context =>
        {
            context.LoggingFields = HttpLoggingFields.None;
        }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);

        Assert.Empty(TestSink.Writes);
    }

    [Fact]
    public async Task RequestInterceptorCanEnableRequestAndResponseLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.None;

        var middleware = CreateMiddleware(RequestResponseApp, options,
            interceptor: new FakeInterceptor(context =>
            {
                context.LoggingFields = HttpLoggingFields.All;
            }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
    }

    [Fact]
    public async Task RequestInterceptorCanAugmentRequestLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;

        var middleware = CreateMiddleware(RequestResponseApp, options,
            interceptor: new FakeInterceptor(context =>
            {
                context.AddParameter("foo", "bar");
            }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("foo: bar"));
    }

    [Fact]
    public async Task RequestInterceptorCanReplaceRequestLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;

        var middleware = CreateMiddleware(RequestResponseApp, options,
            interceptor: new FakeInterceptor(context =>
            {
                Assert.True(context.TryDisable(HttpLoggingFields.RequestPath));
                context.AddParameter("Path", "ReplacedPath");
            }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: ReplacedPath"));
    }

    [Fact]
    public async Task DefaultResponseInfoOnlyHeadersAndRequestInfo()
    {
        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            });

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task ResponseInfoLogsAll()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Response;

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task DurationLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Duration;

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.StartsWith("Duration: ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResponseWithExceptionBeforeBodyLogged()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Response;

        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";

                throw new IOException("Test exception");
            },
            options);

        var httpContext = new DefaultHttpContext();

        await Assert.ThrowsAsync<IOException>(() => middleware.Invoke(httpContext));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
    }

    [Fact]
    public async Task ResponseWithExceptionAfterBodyLogged()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Response;

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");

                throw new IOException("Test exception");
            },
            options);

        var httpContext = new DefaultHttpContext();

        await Assert.ThrowsAsync<IOException>(() => middleware.Invoke(httpContext));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task StatusCodeLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseStatusCode;

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers["Server"] = "Kestrel";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Server: Kestrel"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task ResponseHeadersLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseHeaders;

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task ResponseHeadersRedacted()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseHeaders;

        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.Headers["Test"] = "Kestrel";
                return Task.CompletedTask;
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Test: [Redacted]"));
    }

    [Fact]
    public async Task AllowedResponseHeadersModify()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseHeaders;
        options.CurrentValue.ResponseHeaders.Clear();
        options.CurrentValue.ResponseHeaders.Add("Test");

        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.Headers["Test"] = "Kestrel";
                c.Response.Headers["Server"] = "Kestrel";
                return Task.CompletedTask;
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Test: Kestrel"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Server: [Redacted]"));
    }

    [Theory]
    [MemberData(nameof(BodyData))]
    public async Task ResponseBodyWritingWorks(string expected)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseBody;
        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.ContentType = "text/plain";
                return c.Response.WriteAsync(expected);
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task ResponseBodyNotLoggedIfEmpty()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseBody;
        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.ContentType = "text/plain";
                return Task.CompletedTask;
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);

        Assert.Empty(TestSink.Writes);
    }

    [Fact]
    public async Task ResponseBodyWritingLimitWorks()
    {
        var input = string.Concat(new string('a', 30000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseBody;
        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.ContentType = "text/plain";
                return c.Response.WriteAsync(input);
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);

        var expected = input.Substring(0, options.CurrentValue.ResponseBodyLogLimit);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task FirstWriteResponseHeadersLogged()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Response;

        var writtenHeaders = new TaskCompletionSource();
        var letBodyFinish = new TaskCompletionSource();

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options);

        var httpContext = new DefaultHttpContext();

        var middlewareTask = middleware.Invoke(httpContext);

        await writtenHeaders.Task;

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));

        letBodyFinish.SetResult();

        await middlewareTask;

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task StartAsyncResponseHeadersLogged()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.Response;

        var writtenHeaders = new TaskCompletionSource();
        var letBodyFinish = new TaskCompletionSource();

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.StartAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options);

        var httpContext = new DefaultHttpContext();

        var middlewareTask = middleware.Invoke(httpContext);

        await writtenHeaders.Task;

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));

        letBodyFinish.SetResult();

        await middlewareTask;
    }

    [Fact]
    public async Task UnrecognizedMediaType()
    {
        var expected = "Hello world";
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseBody;
        var middleware = CreateMiddleware(
            c =>
            {
                c.Response.ContentType = "foo/*";
                return c.Response.WriteAsync(expected);
            },
            options);

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Unrecognized Content-Type for response body."));
    }

    [Fact]
    public async Task NoMediaType()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;
        var middleware = CreateMiddleware(RequestResponseApp, options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Remove(HeaderNames.ContentType);

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("No Content-Type header for request body."));
    }

    [Fact]
    public async Task UpgradeToWebSocketLogsResponseStatusCodeWhenResponseIsFlushed()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseStatusCode;

        var writtenHeaders = new TaskCompletionSource();
        var letBodyFinish = new TaskCompletionSource();

        var httpContext = new DefaultHttpContext();

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(true);
        upgradeFeatureMock
            .Setup(m => m.UpgradeAsync())
            .Callback(() =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                httpContext.Response.Headers[HeaderNames.Connection] = HeaderNames.Upgrade;
            })
            .ReturnsAsync(Stream.Null);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options);

        var middlewareTask = middleware.Invoke(httpContext);

        await writtenHeaders.Task;

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 101"));

        letBodyFinish.SetResult();

        await middlewareTask;
    }

    [Fact]
    public async Task UpgradeWithCombineLogs_OneLog()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;
        options.CurrentValue.CombineLogs = true;

        var writtenHeaders = new TaskCompletionSource();
        var letBodyFinish = new TaskCompletionSource();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.1";
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Path = "/";
        httpContext.Request.Headers.Connection = HeaderNames.Upgrade;
        httpContext.Request.Headers.Upgrade = "websocket";

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(true);
        upgradeFeatureMock
            .Setup(m => m.UpgradeAsync())
            .Callback(() =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                httpContext.Response.Headers.Connection = HeaderNames.Upgrade;
            })
            .ReturnsAsync(Stream.Null);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
            },
            options);

        await middleware.Invoke(httpContext);

        Assert.True(TestSink.Writes.TryTake(out var contentTypeLog));
        Assert.Equal("No Content-Type header for request body.", contentTypeLog.Message);

        Assert.True(TestSink.Writes.TryTake(out var requestLog));
        var lines = requestLog.Message.Split(Environment.NewLine);
        var i = 0;
        Assert.Equal("Request and Response:", lines[i++]);
        Assert.Equal("Protocol: HTTP/1.1", lines[i++]);
        Assert.Equal("Method: GET", lines[i++]);
        Assert.Equal("Scheme: http", lines[i++]);
        Assert.Equal("PathBase: ", lines[i++]);
        Assert.Equal("Path: /", lines[i++]);
        Assert.Equal("Connection: Upgrade", lines[i++]);
        Assert.Equal("Upgrade: websocket", lines[i++]);
        Assert.Equal("StatusCode: 101", lines[i++]);
        Assert.Equal("Connection: Upgrade", lines[i++]);
        Assert.StartsWith("Duration: ", lines[i++]);
        Assert.Equal(lines.Length, i);

        Assert.False(TestSink.Writes.TryTake(out var _));
    }

    [Fact]
    public async Task UpgradeToWebSocketLogsResponseHeadersWhenResponseIsFlushed()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseHeaders;

        var writtenHeaders = new TaskCompletionSource();
        var letBodyFinish = new TaskCompletionSource();

        var httpContext = new DefaultHttpContext();

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(true);
        upgradeFeatureMock
            .Setup(m => m.UpgradeAsync())
            .Callback(() =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                httpContext.Response.Headers[HeaderNames.Connection] = HeaderNames.Upgrade;
            })
            .ReturnsAsync(Stream.Null);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options);

        var middlewareTask = middleware.Invoke(httpContext);

        await writtenHeaders.Task;

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: Upgrade"));

        letBodyFinish.SetResult();

        await middlewareTask;
    }

    [Fact]
    public async Task UpgradeToWebSocketDoesNotLogWhenResponseIsFlushedIfLoggingOptionsAreOtherThanResponseStatusCodeOrResponseHeaders()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All ^ HttpLoggingFields.ResponsePropertiesAndHeaders;

        var writtenHeaders = new TaskCompletionSource();
        var letBodyFinish = new TaskCompletionSource();

        var httpContext = new DefaultHttpContext();

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(true);
        upgradeFeatureMock
            .Setup(m => m.UpgradeAsync())
            .Callback(() =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                httpContext.Response.Headers[HeaderNames.Connection] = HeaderNames.Upgrade;
            })
            .ReturnsAsync(Stream.Null);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options);

        var middlewareTask = middleware.Invoke(httpContext);

        await writtenHeaders.Task;

        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("StatusCode: 101"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Connection: Upgrade"));

        letBodyFinish.SetResult();

        await middlewareTask;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogsWrittenOutsideUpgradeWrapperIfUpgradeDoesNotOccur(bool isUpgradableRequest)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponsePropertiesAndHeaders;

        var httpContext = new DefaultHttpContext();

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(isUpgradableRequest);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        var middleware = CreateMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.StartAsync();
            },
            options);

        var middlewareTask = middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Null(
            Record.Exception(() => upgradeFeatureMock.Verify(m => m.UpgradeAsync(), Times.Never)));

        await middlewareTask;
    }

    [Theory]
    [InlineData(HttpLoggingFields.ResponseStatusCode)]
    [InlineData(HttpLoggingFields.ResponseHeaders)]
    public async Task UpgradeToWebSocketLogsWrittenOnlyOnce(HttpLoggingFields loggingFields)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = loggingFields;

        var httpContext = new DefaultHttpContext();

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(true);
        upgradeFeatureMock
            .Setup(m => m.UpgradeAsync())
            .Callback(() =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                httpContext.Response.Headers[HeaderNames.Connection] = HeaderNames.Upgrade;
            })
            .ReturnsAsync(Stream.Null);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        var writeCount = 0;
        TestSink.MessageLogged += (context) => { writeCount++; };

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
            },
            options);

        await middleware.Invoke(httpContext);

        Assert.Equal(1, writeCount);
    }

    [Theory]
    [InlineData(HttpLoggingFields.ResponseStatusCode)]
    [InlineData(HttpLoggingFields.ResponseHeaders)]
    public async Task OriginalUpgradeFeatureIsRestoredBeforeMiddlewareCompletes(HttpLoggingFields loggingFields)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = loggingFields;

        var letBodyFinish = new TaskCompletionSource();

        var httpContext = new DefaultHttpContext();

        var upgradeFeatureMock = new Mock<IHttpUpgradeFeature>();
        upgradeFeatureMock.Setup(m => m.IsUpgradableRequest).Returns(true);
        upgradeFeatureMock.Setup(m => m.UpgradeAsync()).ReturnsAsync(Stream.Null);
        httpContext.Features.Set<IHttpUpgradeFeature>(upgradeFeatureMock.Object);

        IHttpUpgradeFeature upgradeFeature = null;

        var middleware = CreateMiddleware(
            async c =>
            {
                upgradeFeature = c.Features.Get<IHttpUpgradeFeature>();
                await letBodyFinish.Task;
            },
            options);

        var middlewareTask = middleware.Invoke(httpContext);

        Assert.True(upgradeFeature is UpgradeFeatureLoggingDecorator);

        letBodyFinish.SetResult();
        await middlewareTask;

        Assert.False(httpContext.Features.Get<IHttpUpgradeFeature>() is UpgradeFeatureLoggingDecorator);
    }

    [Theory]
    [InlineData(HttpLoggingFields.All, true, true)]
    [InlineData(HttpLoggingFields.All, false, false)]
    [InlineData(HttpLoggingFields.RequestPropertiesAndHeaders, true, true)]
    [InlineData(HttpLoggingFields.RequestPropertiesAndHeaders, false, false)]
    [InlineData(HttpLoggingFields.ResponsePropertiesAndHeaders, true, true)]
    [InlineData(HttpLoggingFields.ResponsePropertiesAndHeaders, false, false)]
    public async Task CombineLogs_OneLog(HttpLoggingFields fields, bool hasRequestBody, bool hasResponseBody)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = fields;
        options.CurrentValue.CombineLogs = true;

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Request.Body.DrainAsync(default);
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                if (hasResponseBody)
                {
                    c.Response.ContentType = "text/plain2";
                    await c.Response.WriteAsync("test response");
                }
            },
            options);

        var httpContext = CreateRequest();
        if (!hasRequestBody)
        {
            httpContext.Request.ContentType = null;
            httpContext.Request.Body = Stream.Null;
        }
        await middleware.Invoke(httpContext);

        var lines = Assert.Single(TestSink.Writes.Where(w => w.LogLevel >= LogLevel.Information)).Message.Split(Environment.NewLine);
        var i = 0;
        Assert.Equal("Request and Response:", lines[i++]);
        if (fields.HasFlag(HttpLoggingFields.RequestPropertiesAndHeaders))
        {
            Assert.Equal("Protocol: HTTP/1.0", lines[i++]);
            Assert.Equal("Method: GET", lines[i++]);
            Assert.Equal("Scheme: http", lines[i++]);
            Assert.Equal("PathBase: /foo", lines[i++]);
            Assert.Equal("Path: /foo", lines[i++]);
            Assert.Equal("Connection: keep-alive", lines[i++]);
            if (hasRequestBody)
            {
                Assert.Equal("Content-Type: text/plain", lines[i++]);
            }
        }
        if (fields.HasFlag(HttpLoggingFields.ResponsePropertiesAndHeaders))
        {
            Assert.Equal("StatusCode: 200", lines[i++]);
            Assert.Equal("Transfer-Encoding: test", lines[i++]);
            if (hasResponseBody)
            {
                Assert.Equal("Content-Type: text/plain2", lines[i++]);
            }
        }
        if (fields.HasFlag(HttpLoggingFields.RequestBody) && hasRequestBody)
        {
            Assert.Equal("RequestBody: test", lines[i++]);
            Assert.Equal("RequestBodyStatus: [Completed]", lines[i++]);
        }
        if (fields.HasFlag(HttpLoggingFields.ResponseBody) && hasResponseBody)
        {
            Assert.Equal("ResponseBody: test response", lines[i++]);
        }
        if (fields.HasFlag(HttpLoggingFields.Duration))
        {
            Assert.StartsWith("Duration: ", lines[i++]);
        }
        Assert.Equal(lines.Length, i);
    }

    [Fact]
    public async Task CombineLogs_Exception_RequestLogged()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;
        options.CurrentValue.CombineLogs = true;

        var middleware = CreateMiddleware(
            async c =>
            {
                await c.Request.Body.DrainAsync(default);
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain2";

                throw new IOException("Test exception");
            },
            options);

        var httpContext = CreateRequest();
        await Assert.ThrowsAsync<IOException>(() => middleware.Invoke(httpContext));

        var lines = Assert.Single(TestSink.Writes).Message.Split(Environment.NewLine);
        var i = 0;
        Assert.Equal("Request and Response:", lines[i++]);
        Assert.Equal("Protocol: HTTP/1.0", lines[i++]);
        Assert.Equal("Method: GET", lines[i++]);
        Assert.Equal("Scheme: http", lines[i++]);
        Assert.Equal("PathBase: /foo", lines[i++]);
        Assert.Equal("Path: /foo", lines[i++]);
        Assert.Equal("Connection: keep-alive", lines[i++]);
        Assert.Equal("Content-Type: text/plain", lines[i++]);
        Assert.Equal("StatusCode: 200", lines[i++]);
        Assert.Equal("Transfer-Encoding: test", lines[i++]);
        Assert.Equal("Content-Type: text/plain2", lines[i++]);
        Assert.Equal("RequestBody: test", lines[i++]);
        Assert.Equal("RequestBodyStatus: [Completed]", lines[i++]);
        Assert.StartsWith("Duration: ", lines[i++]);
        Assert.Equal(lines.Length, i);
    }

    [Fact]
    public async Task ResponseInterceptorCanDisableResponseLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;

        var middleware = CreateMiddleware(RequestResponseApp, options, new FakeInterceptor(_ => { }, context =>
        {
            context.LoggingFields = HttpLoggingFields.None;
        }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));
        // Only response is disabled
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain; p=response"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
    }

    [Fact]
    public async Task ResponseInterceptorCanEnableResponseLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.None;

        var middleware = CreateMiddleware(RequestResponseApp, options,
            interceptor: new FakeInterceptor(_ => { }, context =>
            {
                context.LoggingFields = HttpLoggingFields.All;
            }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));
        // Only Response is enabled
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
    }

    [Fact]
    public async Task ResponseInterceptorCanAugmentResponseLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;

        var middleware = CreateMiddleware(RequestResponseApp, options,
            interceptor: new FakeInterceptor(_ => { }, context =>
            {
                context.AddParameter("foo", "bar");
            }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("foo: bar"));
    }

    [Fact]
    public async Task ResponseInterceptorCanReplaceResponseLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.All;

        var middleware = CreateMiddleware(RequestResponseApp, options,
            interceptor: new FakeInterceptor(_ => { }, context =>
            {
                Assert.True(context.TryDisable(HttpLoggingFields.ResponseStatusCode));
                context.AddParameter("StatusCode", "412");
            }));

        var httpContext = CreateRequest();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("RequestBody: test"));

        Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("StatusCode: 418"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Content-Type: text/plain"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: Hello World"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 412"));
    }

    [Fact]
    public async Task HttpLoggingAttributeWithLessOptionsAppliesToEndpoint()
    {
        var app = CreateApp();
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/attr_responseonly"));

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Request"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
    }

    [Fact]
    public async Task HttpLoggingAttributeWithMoreOptionsAppliesToEndpoint()
    {
        var app = CreateApp(defaultFields: HttpLoggingFields.None);
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/attr_responseandrequest"));

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("Request"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
    }

    [Fact]
    public async Task HttpLoggingAttributeCanRestrictHeaderOutputOnEndpoint()
    {
        var app = CreateApp();
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/attr_restrictedheaders"));

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Scheme"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
    }

    [Fact]
    public async Task HttpLoggingAttributeCanModifyRequestAndResponseSizeOnEndpoint()
    {
        var app = CreateApp();
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/attr_restrictedsize") { Content = new ReadOnlyMemoryContent("from request"u8.ToArray()) };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        var initialResponse = await client.SendAsync(request);

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.Contains(filteredLogs, w => w.Message.Equals("RequestBody: fro[Truncated by RequestBodyLogLimit]"));
        Assert.Contains(filteredLogs, w => w.Message.Equals("ResponseBody: testin"));
    }

    [Fact]
    public async Task InterceptorCanSeeAndOverrideAttributeSettings()
    {
        var app = CreateApp(HttpLoggingFields.None, new FakeInterceptor(requestContext =>
        {
            Assert.Equal(HttpLoggingFields.All, requestContext.LoggingFields);
            requestContext.Disable(HttpLoggingFields.RequestHeaders);
        },
        responseContext =>
        {
            Assert.Equal(HttpLoggingFields.All & ~HttpLoggingFields.RequestHeaders, responseContext.LoggingFields);
            responseContext.Disable(HttpLoggingFields.ResponseHeaders);
        }));
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/attr_responseandrequest");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        var initialResponse = await client.SendAsync(request);

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("Request"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Accept"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Content-Type: text/plain"));
    }

    [Fact]
    public async Task HttpLoggingExtensionWithLessOptionsAppliesToEndpoint()
    {
        var app = CreateApp();
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/ext_responseonly"));

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Request"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
    }

    [Fact]
    public async Task HttpLoggingExtensionWithMoreOptionsAppliesToEndpoint()
    {
        var app = CreateApp(defaultFields: HttpLoggingFields.None);
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/ext_responseandrequest"));

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("Request"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
    }

    [Fact]
    public async Task HttpLoggingExtensionCanRestrictHeaderOutputOnEndpoint()
    {
        var app = CreateApp();
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/ext_restrictedheaders"));

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Scheme"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
    }

    [Fact]
    public async Task HttpLoggingExtensionCanModifyRequestAndResponseSizeOnEndpoint()
    {
        var app = CreateApp();
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/ext_restrictedsize") { Content = new ReadOnlyMemoryContent("from request"u8.ToArray()) };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        var initialResponse = await client.SendAsync(request);

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.Contains(filteredLogs, w => w.Message.Equals("RequestBody: fro[Truncated by RequestBodyLogLimit]"));
        Assert.Contains(filteredLogs, w => w.Message.Equals("ResponseBody: testin"));
    }

    [Fact]
    public async Task InterceptorCanSeeAndOverrideExtensions()
    {
        var app = CreateApp(HttpLoggingFields.None, new FakeInterceptor(requestContext =>
        {
            Assert.Equal(HttpLoggingFields.All, requestContext.LoggingFields);
            requestContext.Disable(HttpLoggingFields.RequestHeaders);
        },
        responseContext =>
        {
            Assert.Equal(HttpLoggingFields.All & ~HttpLoggingFields.RequestHeaders, responseContext.LoggingFields);
            responseContext.Disable(HttpLoggingFields.ResponseHeaders);
        }));
        await app.StartAsync();

        using var server = app.GetTestServer();
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/ext_responseandrequest");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        var initialResponse = await client.SendAsync(request);

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("Request"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Accept"));
        Assert.Contains(filteredLogs, w => w.Message.Contains("StatusCode: 200"));
        Assert.DoesNotContain(filteredLogs, w => w.Message.Contains("Content-Type: text/plain"));
    }

    [Fact]
    public async Task MultipleInterceptorsRun()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All);
                        services.AddHttpLoggingInterceptor<FakeInterceptor0>();
                        services.AddHttpLoggingInterceptor<FakeInterceptor1>();
                        services.AddSingleton(LoggerFactory);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseHttpLogging();
                        app.UseEndpoints(endpoint =>
                        {
                            endpoint.MapGet("/", async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            });
                        });
                    });
            });
        using var host = builder.Build();
        await host.StartAsync();

        using var server = host.GetTestServer();
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        var initialResponse = await client.SendAsync(request);

        var filteredLogs = TestSink.Writes.Where(w => w.LoggerName.Contains("HttpLogging"));

        var requestLog = Assert.Single(filteredLogs, w => w.Message.Contains("Request:"));
        Assert.Contains("i0request: v0", requestLog.Message);
        Assert.Contains("i1request: v1", requestLog.Message);

        var responseLog = Assert.Single(filteredLogs, w => w.Message.Contains("Response:"));
        Assert.Contains("i0response: v0", responseLog.Message);
        Assert.Contains("i1response: v1", responseLog.Message);
    }

    private static DefaultHttpContext CreateRequest()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Path = new PathString("/foo");
        httpContext.Request.PathBase = new PathString("/foo");
        httpContext.Request.QueryString = new QueryString("?foo");
        httpContext.Request.Headers["Connection"] = "keep-alive";
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        return httpContext;
    }

    private IOptionsMonitor<HttpLoggingOptions> CreateOptionsAccessor()
    {
        var options = new HttpLoggingOptions();
        var optionsAccessor = Mock.Of<IOptionsMonitor<HttpLoggingOptions>>(o => o.CurrentValue == options);
        return optionsAccessor;
    }

    private HttpLoggingMiddleware CreateMiddleware(RequestDelegate app = null,
        IOptionsMonitor<HttpLoggingOptions> options = null,
        IHttpLoggingInterceptor interceptor = null)
    {
        return new HttpLoggingMiddleware(
            app ?? (c => Task.CompletedTask),
            options ?? CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>(),
            interceptor == null ? Array.Empty<IHttpLoggingInterceptor>() : [interceptor],
            ObjectPool.Create<HttpLoggingInterceptorContext>(),
            TimeProvider.System);
    }

    private static async Task RequestResponseApp(HttpContext context)
    {
        var arr = new byte[4096];
        while (true)
        {
            var res = await context.Request.Body.ReadAsync(arr);
            if (res == 0)
            {
                break;
            }
        }

        context.Response.StatusCode = StatusCodes.Status418ImATeapot;
        context.Response.ContentType = "text/plain; p=response";
        await context.Response.WriteAsync("Hello World");
    }

    private IHost CreateApp(HttpLoggingFields defaultFields = HttpLoggingFields.All, IHttpLoggingInterceptor interceptor = null)
    {
        var builder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddHttpLogging(o =>
                        {
                            o.LoggingFields = defaultFields;
                        });
                        if (interceptor != null)
                        {
                            services.AddSingleton(interceptor);
                        }
                        services.AddSingleton(LoggerFactory);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseHttpLogging();
                        app.UseEndpoints(endpoint =>
                        {
                            endpoint.MapGet("/attr_responseonly", [HttpLogging(HttpLoggingFields.Response)] async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            });

                            endpoint.MapGet("/ext_responseonly", async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            }).WithHttpLogging(HttpLoggingFields.Response);

                            endpoint.MapGet("/attr_responseandrequest", [HttpLogging(HttpLoggingFields.All)] async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                c.Response.ContentType = "text/plain";
                                return "testing";
                            });

                            endpoint.MapGet("/ext_responseandrequest", async(HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            }).WithHttpLogging(HttpLoggingFields.All);

                            endpoint.MapGet("/attr_restrictedheaders", [HttpLogging((HttpLoggingFields.Request & ~HttpLoggingFields.RequestScheme) | (HttpLoggingFields.Response & ~HttpLoggingFields.ResponseStatusCode))] async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            });

                            endpoint.MapGet("/ext_restrictedheaders", async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            }).WithHttpLogging((HttpLoggingFields.Request & ~HttpLoggingFields.RequestScheme) | (HttpLoggingFields.Response & ~HttpLoggingFields.ResponseStatusCode));

                            endpoint.MapGet("/attr_restrictedsize", [HttpLogging(HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody, RequestBodyLogLimit = 3, ResponseBodyLogLimit = 6)] async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            });

                            endpoint.MapGet("/ext_restrictedsize", async (HttpContext c) =>
                            {
                                await c.Request.Body.ReadAsync(new byte[100]);
                                return "testing";
                            }).WithHttpLogging(HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody, requestBodyLogLimit: 3, responseBodyLogLimit: 6);
                        });
                    });
                });
        return builder.Build();
    }

    private class FakeInterceptor(Action<HttpLoggingInterceptorContext> interceptRequest, Action<HttpLoggingInterceptorContext> interceptResponse = null) : IHttpLoggingInterceptor
    {
        public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
        {
            interceptRequest(logContext);
            return default;
        }

        public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
        {
            interceptResponse?.Invoke(logContext);
            return default;
        }
    }

    private class FakeInterceptor0() : IHttpLoggingInterceptor
    {
        public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
        {
            logContext.AddParameter("i0request", "v0");
            return default;
        }

        public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
        {
            logContext.AddParameter("i0response", "v0");
            return default;
        }
    }

    private class FakeInterceptor1() : IHttpLoggingInterceptor
    {
        public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
        {
            logContext.AddParameter("i1request", "v1");
            return default;
        }

        public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
        {
            logContext.AddParameter("i1response", "v1");
            return default;
        }
    }
}
