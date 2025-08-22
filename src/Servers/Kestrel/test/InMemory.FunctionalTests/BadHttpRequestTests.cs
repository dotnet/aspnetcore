// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class BadHttpRequestTests : LoggedTest
{
    [Theory]
    [MemberData(nameof(InvalidRequestLineData))]
    public Task TestInvalidRequestLines(string request, string expectedExceptionMessage)
    {
        return TestBadRequest(
            request,
            "400 Bad Request",
            expectedExceptionMessage,
            ConnectionEndReason.InvalidRequestLine);
    }

    [Theory]
    [MemberData(nameof(UnrecognizedHttpVersionData))]
    public Task TestInvalidRequestLinesWithUnrecognizedVersion(string httpVersion)
    {
        return TestBadRequest(
            $"GET / {httpVersion}\r\n",
            "505 HTTP Version Not Supported",
            CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(httpVersion),
            ConnectionEndReason.InvalidHttpVersion);
    }

    [Theory]
    [MemberData(nameof(InvalidRequestHeaderData))]
    public Task TestInvalidHeaders(string rawHeaders, string expectedExceptionMessage)
    {
        return TestBadRequest(
            $"GET / HTTP/1.1\r\n{rawHeaders}",
            "400 Bad Request",
            expectedExceptionMessage,
            ConnectionEndReason.InvalidRequestHeaders);
    }

    public static Dictionary<string, (string header, string errorMessage)> BadHeaderData => new Dictionary<string, (string, string)>
        {
            { "Hea\0der: value".EscapeNonPrintable(), ("Hea\0der: value", "Invalid characters in header name.") },
            { "Header: va\0lue".EscapeNonPrintable(), ("Header: va\0lue", "Malformed request: invalid headers.") },
            { "Head\x80r: value".EscapeNonPrintable(), ("Head\x80r: value", "Invalid characters in header name.") },
            { "Header: valu\x80".EscapeNonPrintable(), ("Header: valu\x80", "Malformed request: invalid headers.") },
        };

    public static TheoryData<string> BadHeaderDataNames => new TheoryData<string>
        {
            "Hea\0der: value".EscapeNonPrintable(),
            "Header: va\0lue".EscapeNonPrintable(),
            "Head\x80r: value".EscapeNonPrintable(),
            "Header: valu\x80".EscapeNonPrintable()
        };

    [Theory]
    [MemberData(nameof(BadHeaderDataNames))]
    public Task BadRequestWhenHeaderNameContainsNonASCIIOrNullCharacters(string dataName)
    {
        // Using dictionary of input data to avoid invalid strings in the xml test results
        var header = BadHeaderData[dataName].header;
        var errorMessage = BadHeaderData[dataName].errorMessage;

        return TestBadRequest(
            $"GET / HTTP/1.1\r\n{header}\r\n\r\n",
            "400 Bad Request",
            errorMessage,
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public Task BadRequestIfMethodRequiresLengthButNoContentLengthInHttp10Request(string method)
    {
        return TestBadRequest(
            $"{method} / HTTP/1.0\r\n\r\n",
            "400 Bad Request",
            CoreStrings.FormatBadRequest_LengthRequiredHttp10(method),
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("-1")]
    public Task BadRequestIfContentLengthInvalid(string contentLength)
    {
        return TestBadRequest(
            $"POST / HTTP/1.1\r\nHost:\r\nContent-Length: {contentLength}\r\n\r\n",
            "400 Bad Request",
            CoreStrings.FormatBadRequest_InvalidContentLength_Detail(contentLength),
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [InlineData("GET *", "OPTIONS")]
    [InlineData("GET www.host.com", "CONNECT")]
    public Task RejectsIncorrectMethods(string request, string allowedMethod)
    {
        return TestBadRequest(
            $"{request} HTTP/1.1\r\n",
            "405 Method Not Allowed",
            CoreStrings.BadRequest_MethodNotAllowed,
            ConnectionEndReason.InvalidRequestHeaders,
            $"Allow: {allowedMethod}");
    }

    [Fact]
    public Task BadRequestIfHostHeaderMissing()
    {
        return TestBadRequest(
            "GET / HTTP/1.1\r\n\r\n",
            "400 Bad Request",
            CoreStrings.BadRequest_MissingHostHeader,
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Fact]
    public Task BadRequestIfMultipleHostHeaders()
    {
        return TestBadRequest("GET / HTTP/1.1\r\nHost: localhost\r\nHost: localhost\r\n\r\n",
            "400 Bad Request",
            CoreStrings.BadRequest_MultipleHostHeaders,
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [MemberData(nameof(InvalidHostHeaderData))]
    public Task BadRequestIfHostHeaderDoesNotMatchRequestTarget(string requestTarget, string host)
    {
        return TestBadRequest(
            $"{requestTarget} HTTP/1.1\r\nHost: {host}\r\n\r\n",
            "400 Bad Request",
            CoreStrings.FormatBadRequest_InvalidHostHeader_Detail(host.Trim()),
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [InlineData("http://www.foo.com", "Host: www.foo.comConnection: keep-alive", "www.foo.com")] // Corrupted - missing line-break
    [InlineData("http://www.foo.com/", "Host: www.notfoo.com", "www.foo.com")] // Syntactically correct but not matching
    [InlineData("http://www.foo.com:80", "Host: www.notfoo.com", "www.foo.com")] // Explicit default port in request string
    [InlineData("http://www.foo.com:5129", "Host: www.foo.com", "www.foo.com:5129")] // Non-default port in request string
    [InlineData("http://www.foo.com:5129", "Host: www.foo.com:5128", "www.foo.com:5129")] // Different port in host header
    public async Task CanOptOutOfBadRequestIfHostHeaderDoesNotMatchRequestTarget(string requestString, string hostHeader, string expectedHost)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var receivedHost = StringValues.Empty;
        await using (var server = new TestServer(context =>
        {
            receivedHost = context.Request.Headers.Host;
            return Task.CompletedTask;
        }, new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            ServerOptions = new KestrelServerOptions()
            {
                AllowHostHeaderOverride = true,
            }
        }))
        {
            using (var client = server.CreateConnection())
            {
                await client.SendAll($"GET {requestString} HTTP/1.1\r\n{hostHeader}\r\n\r\n");

                await client.Receive("HTTP/1.1 200 OK");
            }
        }

        Assert.Equal(expectedHost, receivedHost);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Fact]
    public Task BadRequestFor10BadHostHeaderFormat()
    {
        return TestBadRequest(
            $"GET / HTTP/1.0\r\nHost: a=b\r\n\r\n",
            "400 Bad Request",
            CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("a=b"),
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Fact]
    public Task BadRequestFor11BadHostHeaderFormat()
    {
        return TestBadRequest(
            $"GET / HTTP/1.1\r\nHost: a=b\r\n\r\n",
            "400 Bad Request",
            CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("a=b"),
            ConnectionEndReason.InvalidRequestHeaders);
    }

    [Fact]
    public async Task BadRequestLogsAreNotHigherThanDebug()
    {
        await using (var server = new TestServer(async context =>
        {
            await context.Request.Body.ReadAsync(new byte[1], 0, 1);
        }, new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "GET ? HTTP/1.1",
                    "",
                    "");
                await ReceiveBadRequestResponse(connection, "400 Bad Request", server.Context.DateHeaderValue);
            }
        }

        Assert.All(TestSink.Writes.Where(w => w.LoggerName != "Microsoft.Hosting.Lifetime"), w => Assert.InRange(w.LogLevel, LogLevel.Trace, LogLevel.Debug));
        Assert.Contains(TestSink.Writes, w => w.EventId.Id == 17);
    }

    [Fact]
    public async Task TestRequestSplitting()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))))
        {
            using (var client = server.CreateConnection())
            {
                await client.SendAll(
                    "GET /\x0D\0x0ALocation:http://www.contoso.com/ HTTP/1.1",
                    "Host:\r\n\r\n");

                await client.Receive("HTTP/1.1 400");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(),
            m => MetricsAssert.Equal(ConnectionEndReason.InvalidRequestLine, m.Tags));
    }

    [Fact]
    public async Task BadRequestForHttp2()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))))
        {
            using (var client = server.CreateConnection())
            {
                await client.Stream.WriteAsync(Core.Internal.Http2.Http2Connection.ClientPreface.ToArray()).DefaultTimeout();

                var data = await client.Stream.ReadAtLeastLengthAsync(17);

                Assert.Equal(Http1Connection.Http2GoAwayHttp11RequiredBytes.ToArray(), data);
                Assert.Empty(await client.Stream.ReadUntilEndAsync().DefaultTimeout());
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(),
            m => MetricsAssert.Equal(ConnectionEndReason.InvalidHttpVersion, m.Tags));
    }

    [Fact]
    public Task BadRequestForAbsoluteFormTargetWithNonAsciiChars()
    {
        return TestBadRequest(
            $"GET http://localhost/ÿÿÿ HTTP/1.1\r\n",
            "400 Bad Request",
            CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail("http://localhost/\\xFF\\xFF\\xFF"),
            ConnectionEndReason.InvalidRequestLine);
    }

    private class BadRequestEventListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private IDisposable _subscription;
        private Action<KeyValuePair<string, object>> _callback;

        public bool EventFired { get; set; }

        public BadRequestEventListener(DiagnosticListener diagnosticListener, Action<KeyValuePair<string, object>> callback)
        {
            _subscription = diagnosticListener.Subscribe(this, IsEnabled);
            _callback = callback;
        }
        private static readonly Predicate<string> IsEnabled = (provider) => provider switch
        {
            "Microsoft.AspNetCore.Server.Kestrel.BadRequest" => true,
            _ => false
        };
        public void OnNext(KeyValuePair<string, object> pair)
        {
            EventFired = true;
            _callback(pair);
        }
        public void OnError(Exception error) { }
        public void OnCompleted() { }
        public virtual void Dispose() => _subscription.Dispose();
    }

    private async Task TestBadRequest(string request, string expectedResponseStatusCode, string expectedExceptionMessage, ConnectionEndReason reason, string expectedAllowHeader = null)
    {
        BadHttpRequestException loggedException = null;

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ConnectionBadRequest" && context.Exception is BadHttpRequestException ex)
            {
                loggedException = ex;
            }
        };

        // Set up a listener to catch the BadRequest event
        var diagListener = new DiagnosticListener("BadRequestTestsDiagListener");
        string eventProviderName = "";
        string exceptionString = "";
        var badRequestEventListener = new BadRequestEventListener(diagListener, (pair) =>
        {
            eventProviderName = pair.Key;
            var featureCollection = pair.Value as IFeatureCollection;
            if (featureCollection is not null)
            {
                var badRequestFeature = featureCollection.Get<IBadRequestExceptionFeature>();
                exceptionString = badRequestFeature.Error.ToString();
            }
        });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)) { DiagnosticSource = diagListener }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(request);
                await ReceiveBadRequestResponse(connection, expectedResponseStatusCode, server.Context.DateHeaderValue, expectedAllowHeader);
            }
        }

        Assert.NotNull(loggedException);
        Assert.Equal(expectedExceptionMessage, loggedException.Message);

        // Verify DiagnosticSource event for bad request
        Assert.True(badRequestEventListener.EventFired);
        Assert.Equal("Microsoft.AspNetCore.Server.Kestrel.BadRequest", eventProviderName);
        Assert.Contains(expectedExceptionMessage, exceptionString);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(reason, m.Tags));
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\n\r")]
    [InlineData("\n\n")]
    [InlineData("\r\n\r\n")]
    [InlineData("\r\r\r\r\r")]
    public async Task ExtraLinesBetweenRequestsIgnored(string extraLines)
    {
        BadHttpRequestException loggedException = null;

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ConnectionBadRequest" && context.Exception is BadHttpRequestException ex)
            {
                loggedException = ex;
            }
        };

        // Set up a listener to catch the BadRequest event
        var diagListener = new DiagnosticListener("NotBadRequestTestsDiagListener");
        var badRequestEventListener = new BadRequestEventListener(diagListener, (pair) => { });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context => context.Request.Body.DrainAsync(default), new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)) { DiagnosticSource = diagListener }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "funny",
                    extraLines);

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "funny");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                connection.ShutdownSend();

                await connection.ReceiveEnd();
            }
        }

        Assert.Null(loggedException);
        // Verify DiagnosticSource event for bad request
        Assert.False(badRequestEventListener.EventFired);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Fact]
    public async Task ExtraLinesIgnoredBetweenAdjacentRequests()
    {
        BadHttpRequestException loggedException = null;

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ConnectionBadRequest" && context.Exception is BadHttpRequestException ex)
            {
                loggedException = ex;
            }
        };

        // Set up a listener to catch the BadRequest event
        var diagListener = new DiagnosticListener("NotBadRequestTestsDiagListener");
        var badRequestEventListener = new BadRequestEventListener(diagListener, (pair) => { });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context => context.Request.Body.DrainAsync(default), new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)) { DiagnosticSource = diagListener }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "funny",
                    "",
                    "",
                    "",
                    "POST /"); // Split the request line

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                await connection.SendAll(
                    " HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "funny");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                connection.ShutdownSend();

                await connection.ReceiveEnd();
            }
        }

        Assert.Null(loggedException);
        // Verify DiagnosticSource event for bad request
        Assert.False(badRequestEventListener.EventFired);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\n\r")]
    [InlineData("\r\n\r\n")]
    [InlineData("\r\r\r\r\r")]
    public async Task ExtraLinesAtEndOfConnectionIgnored(string extraLines)
    {
        BadHttpRequestException loggedException = null;

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ConnectionBadRequest" && context.Exception is BadHttpRequestException ex)
            {
                loggedException = ex;
            }
        };

        // Set up a listener to catch the BadRequest event
        var diagListener = new DiagnosticListener("NotBadRequestTestsDiagListener");
        var badRequestEventListener = new BadRequestEventListener(diagListener, (pair) => { });

        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context => context.Request.Body.DrainAsync(default), new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)) { DiagnosticSource = diagListener }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 5",
                    "",
                    "funny",
                    extraLines);

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                connection.ShutdownSend();

                await connection.ReceiveEnd();
            }
        }

        Assert.Null(loggedException);
        // Verify DiagnosticSource event for bad request
        Assert.False(badRequestEventListener.EventFired);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    private async Task ReceiveBadRequestResponse(InMemoryConnection connection, string expectedResponseStatusCode, string expectedDateHeaderValue, string expectedAllowHeader = null)
    {
        var lines = new[]
        {
                $"HTTP/1.1 {expectedResponseStatusCode}",
                "Content-Length: 0",
                "Connection: close",
                $"Date: {expectedDateHeaderValue}",
                expectedAllowHeader,
                "",
                ""
            };

        await connection.ReceiveEnd(lines.Where(f => f != null).ToArray());
    }

    public static TheoryData<string, string> InvalidRequestLineData
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var requestLine in HttpParsingData.RequestLineInvalidData)
            {
                var line = requestLine;
                var nullIndex = line.IndexOf('\0');
                if (nullIndex >= 0)
                {
                    line = line.AsSpan().Slice(0, nullIndex + 2).ToString();
                }
                data.Add(requestLine, CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(line[..^1].EscapeNonPrintable()));
            }

            foreach (var target in HttpParsingData.TargetWithEncodedNullCharData)
            {
                data.Add($"GET {target} HTTP/1.1\r\n", CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target.EscapeNonPrintable()));
            }

            foreach (var target in HttpParsingData.TargetWithNullCharData)
            {
                var printableTarget = target.AsSpan().Slice(0, target.IndexOf('\0') + 1).ToString();
                data.Add($"GET {target} HTTP/1.1\r\n", CoreStrings.FormatBadRequest_InvalidRequestLine_Detail($"GET {printableTarget.EscapeNonPrintable()}"));
            }

            return data;
        }
    }

    public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

    public static IEnumerable<object[]> InvalidRequestHeaderData => HttpParsingData.RequestHeaderInvalidData;

    public static IEnumerable<object[]> InvalidRequestHeaderDataLineFeedTerminator => HttpParsingData.RequestHeaderInvalidDataLineFeedTerminator;

    public static TheoryData<string, string> InvalidHostHeaderData => HttpParsingData.HostHeaderInvalidData;
}
