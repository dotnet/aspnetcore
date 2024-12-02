// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using BadHttpRequestException = Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class ChunkedRequestTests : LoggedTest
{
    private async Task App(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        Assert.True(request.CanHaveBody());
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

    private async Task PipeApp(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        Assert.True(request.CanHaveBody());
        while (true)
        {
            var readResult = await request.BodyReader.ReadAsync();
            if (readResult.IsCompleted)
            {
                break;
            }
            // Need to copy here.
            await response.BodyWriter.WriteAsync(readResult.Buffer.ToArray());

            request.BodyReader.AdvanceTo(readResult.Buffer.End);
        }
    }

    private async Task AppChunked(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        Assert.True(request.CanHaveBody());
        var data = new MemoryStream();
        await request.Body.CopyToAsync(data);
        var bytes = data.ToArray();

        response.Headers["Content-Length"] = bytes.Length.ToString(CultureInfo.InvariantCulture);
        await response.Body.WriteAsync(bytes, 0, bytes.Length);
    }

    [Fact]
    public async Task Http10TransferEncoding()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(App, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.0",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "5", "Hello",
                    "6", " World",
                    "0",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task Http10TransferEncodingPipes()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(PipeApp, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.0",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "5", "Hello",
                    "6", " World",
                    "0",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task Http10KeepAliveTransferEncoding()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(AppChunked, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.0",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "Connection: keep-alive",
                    "",
                    "5", "Hello",
                    "6", " World",
                    "0",
                    "",
                    "POST / HTTP/1.0",
                    "Content-Length: 7",
                    "",
                    "Goodbye");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    "Connection: keep-alive",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 7",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Goodbye");
            }
        }
    }

    [Fact]
    public async Task RequestBodyIsConsumedAutomaticallyIfAppDoesntConsumeItFully()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;
            Assert.True(request.CanHaveBody());

            Assert.Equal("POST", request.Method);

            response.Headers["Content-Length"] = new[] { "11" };

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "HelloPOST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "C", "HelloChunked",
                    "0",
                    "",
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 7",
                    "",
                    "Goodbye");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello WorldHTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello WorldHTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task TrailingHeadersAreParsed()
    {
        var requestCount = 10;
        var requestsReceived = 0;

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;
            Assert.True(request.CanHaveBody());

            var buffer = new byte[200];

            // The first request is chunked with no trailers.
            if (requestsReceived == 0)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.False(request.CheckTrailersAvailable(), "CheckTrailersAvailable"); // Not yet
                Assert.Throws<InvalidOperationException>(() => request.GetTrailer("X-Trailer-Header"));  // Not yet
            }
            // The middle requests are chunked with trailers.
            else if (requestsReceived < requestCount)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.False(request.CheckTrailersAvailable(), "CheckTrailersAvailable"); // Not yet
                Assert.Throws<InvalidOperationException>(() => request.GetTrailer("X-Trailer-Header"));  // Not yet
                Assert.Equal("X-Trailer-Header", request.GetDeclaredTrailers().ToString());
            }
            // The last request is content-length with no trailers.
            else
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.False(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Throws<InvalidOperationException>(() => request.GetTrailer("X-Trailer-Header"));
            }

            while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
            {
                ;// read to end
            }

            Assert.False(request.Headers.ContainsKey("X-Trailer-Header"));

            // The first request is chunked with no trailers.
            if (requestsReceived == 0)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.True(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Equal(string.Empty, request.GetDeclaredTrailers().ToString());
                Assert.Equal(string.Empty, request.GetTrailer("X-Trailer-Header").ToString());
            }
            // The middle requests are chunked with trailers.
            else if (requestsReceived < requestCount)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.True(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Equal("X-Trailer-Header", request.GetDeclaredTrailers().ToString());
                Assert.Equal(new string('a', requestsReceived), request.GetTrailer("X-Trailer-Header").ToString());
            }
            // The last request is content-length with no trailers.
            else
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.True(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Equal(string.Empty, request.GetDeclaredTrailers().ToString());
                Assert.Equal(string.Empty, request.GetTrailer("X-Trailer-Header").ToString());
            }

            requestsReceived++;

            response.Headers["Content-Length"] = new[] { "11" };

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
        }, new TestServiceContext(LoggerFactory)))
        {
            var response = string.Join("\r\n", new string[] {
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "Hello World"});

            var expectedFullResponse = string.Join("", Enumerable.Repeat(response, requestCount + 1));

            IEnumerable<string> sendSequence = new string[] {
                    "POST / HTTP/1.1",
                    "Host:",
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
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "Trailer: X-Trailer-Header",
                        "",
                        "C",
                        $"HelloChunk{i:00}",
                        "0",
                        string.Concat("X-Trailer-Header: ", new string('a', i)),
                        "" });
            }

            sendSequence = sendSequence.Concat(new string[] {
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 7",
                    "",
                    "Goodbye"
                });

            var fullRequest = sendSequence.ToArray();

            using (var connection = server.CreateConnection())
            {
                await connection.Send(fullRequest);
                await connection.Receive(expectedFullResponse);
            }
        }
    }

    [Fact]
    public async Task TrailingHeadersAreParsedWithPipe()
    {
        var requestCount = 10;
        var requestsReceived = 0;

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;
            Assert.True(request.CanHaveBody());

            // The first request is chunked with no trailers.
            if (requestsReceived == 0)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.False(request.CheckTrailersAvailable(), "CheckTrailersAvailable"); // Not yet
                Assert.Throws<InvalidOperationException>(() => request.GetTrailer("X-Trailer-Header"));  // Not yet
            }
            // The middle requests are chunked with trailers.
            else if (requestsReceived < requestCount)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.False(request.CheckTrailersAvailable(), "CheckTrailersAvailable"); // Not yet
                Assert.Throws<InvalidOperationException>(() => request.GetTrailer("X-Trailer-Header"));  // Not yet
                Assert.Equal("X-Trailer-Header", request.GetDeclaredTrailers().ToString());
            }
            // The last request is content-length with no trailers.
            else
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.False(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Throws<InvalidOperationException>(() => request.GetTrailer("X-Trailer-Header"));
            }

            while (true)
            {
                var result = await request.BodyReader.ReadAsync();
                request.BodyReader.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }

            Assert.False(request.Headers.ContainsKey("X-Trailer-Header"));

            // The first request is chunked with no trailers.
            if (requestsReceived == 0)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.True(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Equal(string.Empty, request.GetDeclaredTrailers().ToString());
                Assert.Equal(string.Empty, request.GetTrailer("X-Trailer-Header").ToString());
            }
            // The middle requests are chunked with trailers.
            else if (requestsReceived < requestCount)
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.True(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Equal("X-Trailer-Header", request.GetDeclaredTrailers().ToString());
                Assert.Equal(new string('a', requestsReceived), request.GetTrailer("X-Trailer-Header").ToString());
            }
            // The last request is content-length with no trailers.
            else
            {
                Assert.True(request.SupportsTrailers(), "SupportsTrailers");
                Assert.True(request.CheckTrailersAvailable(), "CheckTrailersAvailable");
                Assert.Equal(string.Empty, request.GetDeclaredTrailers().ToString());
                Assert.Equal(string.Empty, request.GetTrailer("X-Trailer-Header").ToString());
            }

            requestsReceived++;

            response.Headers["Content-Length"] = new[] { "11" };

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
        }, new TestServiceContext(LoggerFactory)))
        {
            var response = string.Join("\r\n", new string[] {
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "Hello World"});

            var expectedFullResponse = string.Join("", Enumerable.Repeat(response, requestCount + 1));

            IEnumerable<string> sendSequence = new string[] {
                    "POST / HTTP/1.1",
                    "Host:",
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
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "Trailer: X-Trailer-Header",
                        "",
                        "C",
                        $"HelloChunk{i:00}",
                        "0",
                        string.Concat("X-Trailer-Header: ", new string('a', i)),
                        "" });
            }

            sendSequence = sendSequence.Concat(new string[] {
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 7",
                    "",
                    "Goodbye"
                });

            var fullRequest = sendSequence.ToArray();

            using (var connection = server.CreateConnection())
            {
                await connection.Send(fullRequest);
                await connection.Receive(expectedFullResponse);
            }
        }
    }
    [Fact]
    public async Task TrailingHeadersCountTowardsHeadersTotalSizeLimit()
    {
        const string transferEncodingHeaderLine = "Transfer-Encoding: chunked";
        const string headerLine = "Header: value";
        const string trailingHeaderLine = "Trailing-Header: trailing-value";

        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions.Limits.MaxRequestHeadersTotalSize =
            transferEncodingHeaderLine.Length + 2 +
            headerLine.Length + 2 +
            trailingHeaderLine.Length + 1;

        await using (var server = new TestServer(async context =>
        {
            var buffer = new byte[128];
            while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
            {
                // read to end
            }
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    $"{transferEncodingHeaderLine}",
                    $"{headerLine}",
                    "",
                    "2",
                    "42",
                    "0",
                    $"{trailingHeaderLine}",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 431 Request Header Fields Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task TrailingHeadersCountTowardsHeaderCountLimit()
    {
        const string transferEncodingHeaderLine = "Transfer-Encoding: chunked";
        const string headerLine = "Header: value";
        const string trailingHeaderLine = "Trailing-Header: trailing-value";

        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions.Limits.MaxRequestHeaderCount = 2;

        await using (var server = new TestServer(async context =>
        {
            var buffer = new byte[128];
            while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
            {
                // read to end
            }
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    $"{transferEncodingHeaderLine}",
                    $"{headerLine}",
                    "",
                    "2",
                    "42",
                    "0",
                    $"{trailingHeaderLine}",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 431 Request Header Fields Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ExtensionsAreIgnored()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var requestCount = 10;
        var requestsReceived = 0;

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            var buffer = new byte[200];

            while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
            {
                ;// read to end
            }

            Assert.True(string.IsNullOrEmpty(request.Headers["X-Trailer-Header"]));

            if (requestsReceived < requestCount)
            {
                Assert.Equal(new string('a', requestsReceived), request.GetTrailer("X-Trailer-Header").ToString());
            }

            requestsReceived++;

            response.Headers["Content-Length"] = new[] { "11" };

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
        }, testContext))
        {
            var response = string.Join("\r\n", new string[] {
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World"});

            var expectedFullResponse = string.Join("", Enumerable.Repeat(response, requestCount + 1));

            IEnumerable<string> sendSequence = new string[] {
                    "POST / HTTP/1.1",
                    "Host:",
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
                        "Host:",
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
                    "Host:",
                    "Content-Length: 7",
                    "",
                    "Goodbye"
                });

            var fullRequest = sendSequence.ToArray();

            using (var connection = server.CreateConnection())
            {
                await connection.Send(fullRequest);
                await connection.Receive(expectedFullResponse);
            }
        }
    }

    [Fact]
    public async Task InvalidLengthResultsIn400()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;
            Assert.True(request.CanHaveBody());

            var buffer = new byte[200];

            while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
            {
                ;// read to end
            }

            response.Headers["Content-Length"] = new[] { "11" };

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "Cii");

                await connection.Receive(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    "");
                await connection.ReceiveEnd(
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task InvalidSizedDataResultsIn400()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;
            Assert.True(request.CanHaveBody());

            var buffer = new byte[200];

            while (await request.Body.ReadAsync(buffer, 0, buffer.Length) != 0)
            {
                ;// read to end
            }

            response.Headers["Content-Length"] = new[] { "11" };

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "C",
                    "HelloChunkedIn");

                await connection.Receive(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    "");
                await connection.ReceiveEnd(
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ChunkedNotFinalTransferCodingResultsIn400()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        await using (var server = new TestServer(httpContext =>
        {
            return Task.CompletedTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: not-chunked",
                    "",
                    "C",
                    "hello, world",
                    "0",
                    "",
                    "");

                await connection.ReceiveEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }

            // Content-Length should not affect this
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: not-chunked",
                    "Content-Length: 22",
                    "",
                    "C",
                    "hello, world",
                    "0",
                    "",
                    "");

                await connection.ReceiveEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }

            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked, not-chunked",
                    "",
                    "C",
                    "hello, world",
                    "0",
                    "",
                    "");

                await connection.ReceiveEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }

            // Content-Length should not affect this
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked, not-chunked",
                    "Content-Length: 22",
                    "",
                    "C",
                    "hello, world",
                    "0",
                    "",
                    "");

                await connection.ReceiveEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ClosingConnectionMidChunkPrefixThrows()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));
        var readStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable CS0618 // Type or member is obsolete
        var exTcs = new TaskCompletionSource<BadHttpRequestException>(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async httpContext =>
        {
            var readTask = httpContext.Request.Body.CopyToAsync(Stream.Null);
            readStartedTcs.SetResult();

            try
            {
                await readTask;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            catch (BadHttpRequestException badRequestEx)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                exTcs.TrySetResult(badRequestEx);
            }
            catch (Exception ex)
            {
                exTcs.SetException(ex);
            }
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "1");

                await readStartedTcs.Task.TimeoutAfter(TestConstants.DefaultTimeout);

                connection.ShutdownSend();

                await connection.ReceiveEnd();

                var badReqEx = await exTcs.Task.TimeoutAfter(TestConstants.DefaultTimeout);
                Assert.Equal(RequestRejectionReason.UnexpectedEndOfRequestContent, badReqEx.Reason);
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.UnexpectedEndOfRequestContent, m.Tags));
    }

    [Fact]
    public async Task ChunkedRequestCallCancelPendingReadWorks()
    {
        var tcs = new TaskCompletionSource();
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            Assert.Equal("POST", request.Method);

            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            var requestTask = httpContext.Request.BodyReader.ReadAsync();

            httpContext.Request.BodyReader.CancelPendingRead();

            Assert.True((await requestTask).IsCanceled);

            tcs.SetResult();

            response.Headers["Content-Length"] = new[] { "11" };

            await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "1",
                    "H");
                await tcs.Task;
                await connection.Send(
                    "4",
                    "ello",
                    "0",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task ChunkedRequestCallCompleteThrowsExceptionOnRead()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            Assert.Equal("POST", request.Method);

            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            httpContext.Request.BodyReader.Complete();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await request.BodyReader.ReadAsync());

            response.Headers["Content-Length"] = new[] { "11" };

            await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "1",
                    "H",
                    "4",
                    "ello",
                    "0",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }
    }

    [Fact]
    public async Task ChunkedRequestCallCompleteDoesNotCauseException()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var request = httpContext.Request;

            // This read may receive all data, but what we care about
            // is that ConsumeAsync is called and doesn't error. Calling
            // TryRead before would always fail.
            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            request.BodyReader.Complete();

        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "1",
                    "H",
                    "4",
                    "ello",
                    "0",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");

                // start another request to make sure OnComsumeAsync is hit
                await connection.Send(
                   "POST / HTTP/1.1",
                   "Host:",
                   "Transfer-Encoding: chunked",
                   "",
                   "0",
                   "",
                   "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.All(TestSink.Writes, w => Assert.InRange(w.LogLevel, LogLevel.Trace, LogLevel.Information));
    }

    [Fact]
    public async Task ChunkedRequestCallCompleteWithExceptionCauses500()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            var request = httpContext.Request;

            Assert.Equal("POST", request.Method);

            var readResult = await request.BodyReader.ReadAsync();
            request.BodyReader.AdvanceTo(readResult.Buffer.End);

            httpContext.Request.BodyReader.Complete(new Exception());

            response.Headers["Content-Length"] = new[] { "11" };

            await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "1",
                    "H",
                    "0",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 500 Internal Server Error",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }
}
