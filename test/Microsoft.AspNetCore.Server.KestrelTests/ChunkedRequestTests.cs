// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
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

        [Theory]
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

        [Theory]
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

        [Theory]
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

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task TrailingHeadersAreParsed(ServiceContext testContext)
        {
            var requestCount = 10;
            var requestsReceived = 0;

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;

                var buffer = new byte[200];

                Assert.True(string.IsNullOrEmpty(request.Headers["X-Trailer-Header"]));

                while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
                {
                    // read to end
                }

                if (requestsReceived < requestCount)
                {
                    Assert.Equal(new string('a', requestsReceived), request.Headers["X-Trailer-Header"].ToString());
                }
                else
                {
                    Assert.True(string.IsNullOrEmpty(request.Headers["X-Trailer-Header"]));
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

                IEnumerable<string> sendSequence = new string[] {
                    "POST / HTTP/1.1",
                    "Transfer-Encoding: chunked",
                    "",
                    "C", 
                    "HelloChunked",
                    "0",
                    ""};

                for (var i = 1; i < requestCount; i++)
                {
                    sendSequence = sendSequence.Concat(new string[] {
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "C",
                        $"HelloChunk{i:00}",
                        "0",
                        string.Concat("X-Trailer-Header: ", new string('a', i)),
                        "" });
                }

                sendSequence = sendSequence.Concat(new string[] {
                    "POST / HTTP/1.1",
                    "Content-Length: 7",
                    "",
                    "Goodbye"
                });

                var fullRequest = sendSequence.ToArray();

                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(fullRequest);

                    await connection.ReceiveEnd(expectedFullResponse);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ExtensionsAreIgnored(ServiceContext testContext)
        {
            var requestCount = 10;
            var requestsReceived = 0;

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;

                var buffer = new byte[200];

                Assert.True(string.IsNullOrEmpty(request.Headers["X-Trailer-Header"]));

                while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
                {
                    // read to end
                }

                if (requestsReceived < requestCount)
                {
                    Assert.Equal(new string('a', requestsReceived), request.Headers["X-Trailer-Header"].ToString());
                }
                else
                {
                    Assert.True(string.IsNullOrEmpty(request.Headers["X-Trailer-Header"]));
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

                IEnumerable<string> sendSequence = new string[] {
                    "POST / HTTP/1.1",
                    "Transfer-Encoding: chunked",
                    "",
                    "C;hello there",
                    "HelloChunked",
                    "0;hello there",
                    ""};

                for (var i = 1; i < requestCount; i++)
                {
                    sendSequence = sendSequence.Concat(new string[] {
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "C;hello there",
                        $"HelloChunk{i:00}",
                        "0;hello there",
                        string.Concat("X-Trailer-Header: ", new string('a', i)),
                        "" });
                }

                sendSequence = sendSequence.Concat(new string[] {
                    "POST / HTTP/1.1",
                    "Content-Length: 7",
                    "",
                    "Goodbye"
                });

                var fullRequest = sendSequence.ToArray();

                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(fullRequest);

                    await connection.ReceiveEnd(expectedFullResponse);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task InvalidLengthResultsIn500(ServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;

                var buffer = new byte[200];

                while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
                {
                    ;// read to end
                }

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                    "POST / HTTP/1.1",
                    "Transfer-Encoding: chunked",
                    "",
                    "Cio",
                    "HelloChunked",
                    "0",
                    "");

                    // Should really be a 40x as is bad request
                    await connection.Receive(
                        "HTTP/1.1 500 Internal Server Error",
                        "");
                    await connection.ReceiveStartsWith("Date:");
                    await connection.ReceiveEnd(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task InvalidSizedDataResultsIn500(ServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;

                var buffer = new byte[200];

                while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
                {
                    ;// read to end
                }

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "C",
                        "HelloChunkedInvalid",
                        "0",
                        "");

                    // Should really be a 40x as is bad request
                    await connection.Receive(
                        "HTTP/1.1 500 Internal Server Error",
                        "");
                    await connection.ReceiveStartsWith("Date:");
                    await connection.ReceiveEnd(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "",
                        "");
                }
            }
        }
    }
}

