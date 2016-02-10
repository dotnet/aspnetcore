// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ChunkedRequestTests
    {
        public static TheoryData<ServiceContext> ConnectionFilterData
        {
            get
            {
                return new TheoryData<ServiceContext>
                {
                    {
                        new TestServiceContext()
                    },
                    {
                        new TestServiceContext(new PassThroughConnectionFilter())
                    }
                };
            }
        }

        private async Task App(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            response.Headers.Clear();
            while (true)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
            }
        }

        private async Task AppChunked(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            var data = new MemoryStream();
            await request.Body.CopyToAsync(data);
            var bytes = data.ToArray();

            response.Headers.Clear();
            response.Headers["Content-Length"] = bytes.Length.ToString();
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        [ConditionalTheory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10TransferEncoding(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "Transfer-Encoding: chunked",
                        "",
                        "5", "Hello",
                        "6", " World",
                        "0",
                         "");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "",
                        "Hello World");
                }
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAliveTransferEncoding(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "Transfer-Encoding: chunked",
                        "Connection: keep-alive",
                        "",
                        "5", "Hello",
                        "6", " World",
                        "0",
                         "",
                        "POST / HTTP/1.0",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Connection: keep-alive",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task RequestBodyIsConsumedAutomaticallyIfAppDoesntConsumeItFully(ServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;

                Assert.Equal("POST", request.Method);

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.1",
                        "Content-Length: 5",
                        "",
                        "HelloPOST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "C", "HelloChunked",
                        "0",
                        "",
                        "POST / HTTP/1.1",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello WorldHTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello WorldHTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(ConnectionFilterData))]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task TrailingHeadersAreParsed(ServiceContext testContext)
        {
            var requestCount = 10;
            var requestsReceived = 0;

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;

                var buffer = new byte[200];

                Assert.Equal(string.Empty, request.Headers["X-Trailer-Header"]);

                while(await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
                {
                    // read to end
                }

                if (requestsReceived < requestCount)
                {
                    Assert.Equal(new string('a', requestsReceived), request.Headers["X-Trailer-Header"]);
                }
                else
                {
                    Assert.Equal(string.Empty, request.Headers["X-Trailer-Header"]);
                }

                requestsReceived++;

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                var response = string.Join("\r\n", new string[] {
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    "",
                    "Hello World"});

                var expectedFullResponse = string.Join("", Enumerable.Repeat(response, requestCount + 1));

                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "C", "HelloChunked",
                        "0",
                        "");

                    for (var i = 1; i < requestCount; i++)
                    {
                        await connection.Send(
                            "POST / HTTP/1.1",
                            "Transfer-Encoding: chunked",
                            "",
                            "C", "HelloChunked",
                            "0",
                            string.Concat("X-Trailer-Header", new string('a', i)),
                            "");
                    }

                    await connection.SendEnd(
                        "POST / HTTP/1.1",
                        "Content-Length: 7",
                        "",
                        "Goodbye");

                    await connection.ReceiveEnd(expectedFullResponse);
                }
            }
        }

        private class TestApplicationErrorLogger : ILogger
        {
            public int ApplicationErrorsLogged { get; set; }

            public IDisposable BeginScopeImpl(object state)
            {
                return new Disposable(() => { });
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                // Application errors are logged using 13 as the eventId.
                if (eventId.Id == 13)
                {
                    ApplicationErrorsLogged++;
                }
            }
        }

        private class PassThroughConnectionFilter : IConnectionFilter
        {
            public Task OnConnectionAsync(ConnectionFilterContext context)
            {
                context.Connection = new LoggingStream(context.Connection, new TestApplicationErrorLogger());
                return TaskUtilities.CompletedTask;
            }
        }
    }
}

