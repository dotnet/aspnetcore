// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.HttpLogging;

public class HttpLoggingMiddlewareTests : LoggedTest
{
    public static TheoryData BodyData
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
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>()));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        null,
        LoggerFactory.CreateLogger<HttpLoggingMiddleware>()));

        Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        CreateOptionsAccessor(),
        null));
    }

    [Fact]
    public async Task NoopWhenLoggingDisabled()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.None;

        var middleware = new HttpLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        await middleware.Invoke(httpContext);

        Assert.Empty(TestSink.Writes);
    }

    [Fact]
    public async Task DefaultRequestInfoOnlyHeadersAndRequestInfo()
    {
        var middleware = new HttpLoggingMiddleware(
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
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Path = new PathString("/foo");
        httpContext.Request.PathBase = new PathString("/foo");
        httpContext.Request.Headers["Connection"] = "keep-alive";
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("test"));

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
        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Path = new PathString("/foo");
        httpContext.Request.PathBase = new PathString("/foo");
        httpContext.Request.Headers["Connection"] = "keep-alive";
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("test"));

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
        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Path = new PathString("/foo");
        httpContext.Request.PathBase = new PathString("/foo");
        httpContext.Request.Headers["Connection"] = "keep-alive";
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("test"));

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
        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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
        var middleware = new HttpLoggingMiddleware(
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
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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
        var middleware = new HttpLoggingMiddleware(
             c =>
             {
                 return Task.CompletedTask;
             },
             options,
             LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

    [Theory]
    [MemberData(nameof(BodyData))]
    public async Task RequestBodyReadingWorks(string expected)
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, options.CurrentValue.RequestBodyLogLimit / 3);

        Assert.Contains(TestSink.Writes, w => w.Message.Equals("RequestBody: " + expected));
    }

    [Fact]
    public async Task RequestBodyReadingLimitWorks()
    {
        var input = string.Concat(new string('a', 60000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, options.CurrentValue.ResponseBodyLogLimit);

        Assert.Contains(TestSink.Writes, w => w.Message.Equals("RequestBody: " + expected));
    }

    [Fact]
    public async Task PartialReadBodyStillLogs()
    {
        var input = string.Concat(new string('a', 60000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                var arr = new byte[4096];
                var res = await c.Request.Body.ReadAsync(arr);
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

        await middleware.Invoke(httpContext);
        var expected = input.Substring(0, 4096);

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

        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = contentType;
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Theory]
    [InlineData("application/invalid")]
    [InlineData("multipart/form-data")]
    public async Task RejectedContentTypes(string contentType)
    {
        // media headers that should work.
        var expected = new string('a', 1000);
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;

        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/plain";
        httpContext.Request.Body = new MemoryStream(encoding.GetBytes(expected));

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task DefaultResponseInfoOnlyHeadersAndRequestInfo()
    {
        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            CreateOptionsAccessor(),
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Transfer-Encoding: test"));
        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
    }

    [Fact]
    public async Task StatusCodeLogs()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseStatusCode;

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers["Server"] = "Kestrel";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            c =>
            {
                c.Response.Headers["Test"] = "Kestrel";
                return Task.CompletedTask;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            c =>
            {
                c.Response.Headers["Test"] = "Kestrel";
                c.Response.Headers["Server"] = "Kestrel";
                return Task.CompletedTask;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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
        var middleware = new HttpLoggingMiddleware(
            c =>
            {
                c.Response.ContentType = "text/plain";
                return c.Response.WriteAsync(expected);
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
    }

    [Fact]
    public async Task ResponseBodyWritingLimitWorks()
    {
        var input = string.Concat(new string('a', 30000), new string('b', 3000));
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.ResponseBody;
        var middleware = new HttpLoggingMiddleware(
            c =>
            {
                c.Response.ContentType = "text/plain";
                return c.Response.WriteAsync(input);
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.WriteAsync("test");
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.StartAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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
        var middleware = new HttpLoggingMiddleware(
            c =>
            {
                c.Response.ContentType = "foo/*";
                return c.Response.WriteAsync(expected);
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();

        await middleware.Invoke(httpContext);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("Unrecognized Content-Type for response body."));
    }

    [Fact]
    public async Task NoMediaType()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = HttpLoggingFields.RequestBody;
        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Request.ContentType = null;
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
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var httpContext = new DefaultHttpContext();

        httpContext.Request.Headers["foo"] = "bar";

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var middlewareTask = middleware.Invoke(httpContext);

        await writtenHeaders.Task;

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 101"));

        letBodyFinish.SetResult();

        await middlewareTask;
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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                writtenHeaders.SetResult();
                await letBodyFinish.Task;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                c.Response.StatusCode = 200;
                c.Response.Headers[HeaderNames.TransferEncoding] = "test";
                c.Response.ContentType = "text/plain";
                await c.Response.StartAsync();
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                await c.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

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

        var middleware = new HttpLoggingMiddleware(
            async c =>
            {
                upgradeFeature = c.Features.Get<IHttpUpgradeFeature>();
                await letBodyFinish.Task;
            },
            options,
            LoggerFactory.CreateLogger<HttpLoggingMiddleware>());

        var middlewareTask = middleware.Invoke(httpContext);

        Assert.True(upgradeFeature is UpgradeFeatureLoggingDecorator);

        letBodyFinish.SetResult();
        await middlewareTask;

        Assert.False(httpContext.Features.Get<IHttpUpgradeFeature>() is UpgradeFeatureLoggingDecorator);
    }

    private IOptionsMonitor<HttpLoggingOptions> CreateOptionsAccessor()
    {
        var options = new HttpLoggingOptions();
        var optionsAccessor = Mock.Of<IOptionsMonitor<HttpLoggingOptions>>(o => o.CurrentValue == options);
        return optionsAccessor;
    }
}
