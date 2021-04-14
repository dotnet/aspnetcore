// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.HttpsPolicy.Tests
{
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
            Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(null, CreateOptionsAccessor(), LoggerFactory));

            Assert.Throws<ArgumentNullException>(() => new HttpLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            null,
            LoggerFactory));

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
            options.Value.LoggingFields = HttpLoggingFields.None;

            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    c.Response.StatusCode = 200;
                    return Task.CompletedTask;
                },
                options,
                LoggerFactory);

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
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
        }

        [Fact]
        public async Task DefaultRequestInfoOnlyHeadersAndRequestInfo()
        {
            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    return Task.CompletedTask;
                },
                CreateOptionsAccessor(),
                LoggerFactory);

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
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("QueryString: ?foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
        }

        [Fact]
        public async Task RequestLogsAllRequestInfo()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.Request;
            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    return Task.CompletedTask;
                },
                options,
                LoggerFactory);

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
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("QueryString: ?foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
        }

        [Fact]
        public async Task RequestPropertiesLogs()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestProperties;
            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    return Task.CompletedTask;
                },
                options,
                LoggerFactory);

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
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Protocol: HTTP/1.0"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Method: GET"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Scheme: http"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Path: /foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("PathBase: /foo"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("QueryString: ?foo"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Connection: keep-alive"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
        }

        [Fact]
        public async Task RequestHeadersLogs()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestHeaders;
            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    return Task.CompletedTask;
                },
                options,
                LoggerFactory);

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
                c =>
                {
                    return Task.CompletedTask;
                },
                CreateOptionsAccessor(),
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            httpContext.Request.Headers["foo"] = "bar";

            await middleware.Invoke(httpContext);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("foo: X"));
        }

        [Fact]
        public async Task CanConfigureRequestAllowList()
        {
            var options = CreateOptionsAccessor();
            options.Value.AllowedRequestHeaders.Clear();
            options.Value.AllowedRequestHeaders.Add("foo");
            var middleware = new HttpLoggingMiddleware(
                 c =>
                 {
                     return Task.CompletedTask;
                 },
                 options,
                 LoggerFactory);

            var httpContext = new DefaultHttpContext();

            // Header on the default allow list.
            httpContext.Request.Headers["Connection"] = "keep-alive";

            httpContext.Request.Headers["foo"] = "bar";

            await middleware.Invoke(httpContext);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("foo: bar"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Connection: X"));
        }

        [Theory]
        [MemberData(nameof(BodyData))]
        public async Task RequestBodyReadingWorks(string expected)
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/plain";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

            await middleware.Invoke(httpContext);

            Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
        }

        [Fact]
        public async Task RequestBodyReadingLimitWorks()
        {
            var input = string.Concat(new string('a', 30000), new string('b', 3000));
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/plain";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

            await middleware.Invoke(httpContext);
            var expected = input.Substring(0, options.Value.ResponseBodyLogLimit);

            Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
        }

        [Fact]
        public async Task RequestBodyReadingLimitGreaterThanPipeWorks()
        {
            var input = string.Concat(new string('a', 60000), new string('b', 3000));
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/plain";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));

            await middleware.Invoke(httpContext);
            var expected = input.Substring(0, options.Value.ResponseBodyLogLimit);

            Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
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
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;

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
                LoggerFactory);

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
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(expected));

            await middleware.Invoke(httpContext);

            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains(expected));
        }

        [Fact]
        public async Task DifferentEncodingsWork()
        {
            var encoding = Encoding.Unicode;
            var expected = new string('a', 1000);
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;
            options.Value.BodyEncoding = encoding;

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/plain";
            httpContext.Request.Body = new MemoryStream(encoding.GetBytes(expected));

            await middleware.Invoke(httpContext);

            Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
        }

        [Fact]
        public async Task DefaultBodyTimeoutTruncates()
        {
            // media headers that should work.
            var expected = new string('a', 1000);
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/plain";
            httpContext.Request.Body = new SlowStream(new MemoryStream(Encoding.ASCII.GetBytes("test")), TimeSpan.FromSeconds(2));

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await middleware.Invoke(httpContext));
        }

        [Fact]
        public async Task CanSetTimeoutToDifferentValue()
        {
            // media headers that should work.
            var expected = new string('a', 1000);
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.RequestBody;
            options.Value.RequestBodyTimeout = TimeSpan.FromSeconds(30);

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
                LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/plain";
            // Does not timeout.
            httpContext.Request.Body = new SlowStream(new MemoryStream(Encoding.ASCII.GetBytes("test")), TimeSpan.FromSeconds(2));

            await middleware.Invoke(httpContext);
        }

        [Fact]
        public async Task DefaultResponseInfoOnlyHeadersAndRequestInfo()
        {
            var middleware = new HttpLoggingMiddleware(
                async c =>
                {
                    c.Response.StatusCode = 200;
                    c.Response.Headers["Server"] = "Kestrel";
                    c.Response.ContentType = "text/plain";
                    await c.Response.WriteAsync("test");
                },
                CreateOptionsAccessor(),
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Server: Kestrel"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
        }

        [Fact]
        public async Task ResponseInfoLogsAll()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.Response;

            var middleware = new HttpLoggingMiddleware(
                async c =>
                {
                    c.Response.StatusCode = 200;
                    c.Response.Headers["Server"] = "Kestrel";
                    c.Response.ContentType = "text/plain";
                    await c.Response.WriteAsync("test");
                },
                options,
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Server: Kestrel"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Body: test"));
        }


        [Fact]
        public async Task StatusCodeLogs()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.StatusCode;

            var middleware = new HttpLoggingMiddleware(
                async c =>
                {
                    c.Response.StatusCode = 200;
                    c.Response.Headers["Server"] = "Kestrel";
                    c.Response.ContentType = "text/plain";
                    await c.Response.WriteAsync("test");
                },
                options,
                LoggerFactory);

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
            options.Value.LoggingFields = HttpLoggingFields.ResponseHeaders;

            var middleware = new HttpLoggingMiddleware(
                async c =>
                {
                    c.Response.StatusCode = 200;
                    c.Response.Headers["Server"] = "Kestrel";
                    c.Response.ContentType = "text/plain";
                    await c.Response.WriteAsync("test");
                },
                options,
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("StatusCode: 200"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Server: Kestrel"));
            Assert.DoesNotContain(TestSink.Writes, w => w.Message.Contains("Body: test"));
        }

        [Fact]
        public async Task ResponseHeadersRedacted()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.ResponseHeaders;

            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    c.Response.Headers["Test"] = "Kestrel";
                    return Task.CompletedTask;
                },
                options,
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Test: X"));
        }

        [Fact]
        public async Task AllowedResponseHeadersModify()
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.ResponseHeaders;
            options.Value.AllowedResponseHeaders.Clear();
            options.Value.AllowedResponseHeaders.Add("Test");

            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    c.Response.Headers["Test"] = "Kestrel";
                    c.Response.Headers["Server"] = "Kestrel";
                    return Task.CompletedTask;
                },
                options,
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Test: Kestrel"));
            Assert.Contains(TestSink.Writes, w => w.Message.Contains("Server: X"));
        }

        [Theory]
        [MemberData(nameof(BodyData))]
        public async Task ResponseBodyWritingWorks(string expected)
        {
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.ResponseBody;
            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    c.Response.ContentType = "text/plain";
                    return c.Response.WriteAsync(expected);
                },
                options,
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);

            Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
        }

        [Fact]
        public async Task ResponseBodyWritingLimitWorks()
        {
            var input = string.Concat(new string('a', 30000), new string('b', 3000));
            var options = CreateOptionsAccessor();
            options.Value.LoggingFields = HttpLoggingFields.ResponseBody;
            var middleware = new HttpLoggingMiddleware(
                c =>
                {
                    c.Response.ContentType = "text/plain";
                    return c.Response.WriteAsync(input);
                },
                options,
                LoggerFactory);

            var httpContext = new DefaultHttpContext();

            await middleware.Invoke(httpContext);

            var expected = input.Substring(0, options.Value.ResponseBodyLogLimit);
            Assert.Contains(TestSink.Writes, w => w.Message.Contains(expected));
        }

        private IOptions<HttpLoggingOptions> CreateOptionsAccessor()
        {
            var options = new HttpLoggingOptions();
            var optionsAccessor = Mock.Of<IOptions<HttpLoggingOptions>>(o => o.Value == options);
            return optionsAccessor;
        }

        private class SlowStream : Stream
        {
            private readonly Stream _inner;
            private readonly TimeSpan _artificialDelay;

            public SlowStream(Stream inner, TimeSpan artificialDelay)
            {
                _inner = inner;
                _artificialDelay = artificialDelay;
            }

            public override void Flush()
            {
                _inner.Flush();
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                await Task.Delay(_artificialDelay);
                return await _inner.ReadAsync(buffer, offset, count, token);
            }

            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position
            {
                get => _inner.Position;
                set => _inner.Position = value;
            }
        }
    }
}
