// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameTests : IDisposable
    {
        private readonly SocketInput _socketInput;
        private readonly MemoryPool _pool;
        private readonly Frame<object> _frame;
        private readonly ServiceContext _serviceContext;
        private readonly ConnectionContext _connectionContext;

        public FrameTests()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            _pool = new MemoryPool();
            _socketInput = new SocketInput(_pool, ltp);

            _serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = trace
            };
            var listenerContext = new ListenerContext(_serviceContext)
            {
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000")
            };
            _connectionContext = new ConnectionContext(listenerContext)
            {
                Input = _socketInput,
                Output = new MockSocketOuptut(),
                ConnectionControl = Mock.Of<IConnectionControl>()
            };

            _frame = new Frame<object>(application: null, context: _connectionContext);
            _frame.Reset();
            _frame.InitializeHeaders();
        }

        public void Dispose()
        {
            _pool.Dispose();
            _socketInput.Dispose();
        }

        [Fact]
        public void CanReadHeaderValueWithoutLeadingWhitespace()
        {
            _frame.InitializeHeaders();

            var headerArray = Encoding.ASCII.GetBytes("Header:value\r\n\r\n");
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var success = _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value", _frame.RequestHeaders["Header"]);

            // Assert TakeMessageHeaders consumed all the input
            var scan = _socketInput.ConsumingStart();
            Assert.True(scan.IsEnd);
        }

        [Theory]
        [InlineData("Header: value\r\n\r\n")]
        [InlineData("Header:  value\r\n\r\n")]
        [InlineData("Header:\tvalue\r\n\r\n")]
        [InlineData("Header: \tvalue\r\n\r\n")]
        [InlineData("Header:\t value\r\n\r\n")]
        [InlineData("Header:\t\tvalue\r\n\r\n")]
        [InlineData("Header:\t\t value\r\n\r\n")]
        [InlineData("Header: \t\tvalue\r\n\r\n")]
        [InlineData("Header: \t\t value\r\n\r\n")]
        [InlineData("Header: \t \t value\r\n\r\n")]
        public void LeadingWhitespaceIsNotIncludedInHeaderValue(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var success = _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value", _frame.RequestHeaders["Header"]);

            // Assert TakeMessageHeaders consumed all the input
            var scan = _socketInput.ConsumingStart();
            Assert.True(scan.IsEnd);
        }

        [Theory]
        [InlineData("Header: value \r\n\r\n")]
        [InlineData("Header: value\t\r\n\r\n")]
        [InlineData("Header: value \t\r\n\r\n")]
        [InlineData("Header: value\t \r\n\r\n")]
        [InlineData("Header: value\t\t\r\n\r\n")]
        [InlineData("Header: value\t\t \r\n\r\n")]
        [InlineData("Header: value \t\t\r\n\r\n")]
        [InlineData("Header: value \t\t \r\n\r\n")]
        [InlineData("Header: value \t \t \r\n\r\n")]
        public void TrailingWhitespaceIsNotIncludedInHeaderValue(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var success = _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value", _frame.RequestHeaders["Header"]);

            // Assert TakeMessageHeaders consumed all the input
            var scan = _socketInput.ConsumingStart();
            Assert.True(scan.IsEnd);
        }

        [Theory]
        [InlineData("Header: one two three\r\n\r\n", "one two three")]
        [InlineData("Header: one  two  three\r\n\r\n", "one  two  three")]
        [InlineData("Header: one\ttwo\tthree\r\n\r\n", "one\ttwo\tthree")]
        [InlineData("Header: one two\tthree\r\n\r\n", "one two\tthree")]
        [InlineData("Header: one\ttwo three\r\n\r\n", "one\ttwo three")]
        [InlineData("Header: one \ttwo \tthree\r\n\r\n", "one \ttwo \tthree")]
        [InlineData("Header: one\t two\t three\r\n\r\n", "one\t two\t three")]
        [InlineData("Header: one \ttwo\t three\r\n\r\n", "one \ttwo\t three")]
        public void WhitespaceWithinHeaderValueIsPreserved(string rawHeaders, string expectedValue)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var success = _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal(expectedValue, _frame.RequestHeaders["Header"]);

            // Assert TakeMessageHeaders consumed all the input
            var scan = _socketInput.ConsumingStart();
            Assert.True(scan.IsEnd);
        }

        [Theory]
        [InlineData("Header: line1\r\n line2\r\n\r\n")]
        [InlineData("Header: line1\r\n\tline2\r\n\r\n")]
        [InlineData("Header: line1\r\n  line2\r\n\r\n")]
        [InlineData("Header: line1\r\n \tline2\r\n\r\n")]
        [InlineData("Header: line1\r\n\t line2\r\n\r\n")]
        [InlineData("Header: line1\r\n\t\tline2\r\n\r\n")]
        [InlineData("Header: line1\r\n \t\t line2\r\n\r\n")]
        [InlineData("Header: line1\r\n \t \t line2\r\n\r\n")]
        public void TakeMessageHeadersThrowsOnHeaderValueWithLineFolding(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Header value line folding not supported.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Fact]
        public void TakeMessageHeadersThrowsOnHeaderValueWithLineFolding_CharacterNotAvailableOnFirstAttempt()
        {
            var headerArray = Encoding.ASCII.GetBytes("Header-1: value1\r\n");
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            Assert.False(_frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));

            _socketInput.IncomingData(Encoding.ASCII.GetBytes(" "), 0, 1);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Header value line folding not supported.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Theory]
        [InlineData("Header-1: value1\r\r\n")]
        [InlineData("Header-1: val\rue1\r\n")]
        [InlineData("Header-1: value1\rHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: v\ralue2\r\n")]
        public void TakeMessageHeadersThrowsOnHeaderValueContainingCR(string rawHeaders)
        {

            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Header value must not contain CR characters.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Theory]
        [InlineData("Header-1 value1\r\n\r\n")]
        [InlineData("Header-1 value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 value2\r\n\r\n")]
        public void TakeMessageHeadersThrowsOnHeaderLineMissingColon(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("No ':' character found in header line.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Theory]
        [InlineData(" Header: value\r\n\r\n")]
        [InlineData("\tHeader: value\r\n\r\n")]
        [InlineData(" Header-1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("\tHeader-1: value1\r\nHeader-2: value2\r\n\r\n")]
        public void TakeMessageHeadersThrowsOnHeaderLineStartingWithWhitespace(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Header line must not start with whitespace.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Theory]
        [InlineData("Header : value\r\n\r\n")]
        [InlineData("Header\t: value\r\n\r\n")]
        [InlineData("Header 1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header 1 : value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header 1\t: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader 2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 : value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2\t: value2\r\n\r\n")]
        public void TakeMessageHeadersThrowsOnWhitespaceInHeaderName(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Whitespace is not allowed in header name.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Theory]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r\r")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r ")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r \n")]
        public void TakeMessageHeadersThrowsOnHeadersNotEndingInCRLFLine(string rawHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Headers corrupted, invalid header sequence.", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Fact]
        public void TakeMessageHeadersThrowsWhenHeadersExceedTotalSizeLimit()
        {
            const string headerLine = "Header: value\r\n";
            _serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize = headerLine.Length - 1;
            _frame.Reset();

            var headerArray = Encoding.ASCII.GetBytes($"{headerLine}\r\n");
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Request headers too long.", exception.Message);
            Assert.Equal(431, exception.StatusCode);
        }

        [Fact]
        public void TakeMessageHeadersThrowsWhenHeadersExceedCountLimit()
        {
            const string headerLines = "Header-1: value1\r\nHeader-2: value2\r\n";
            _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = 1;

            var headerArray = Encoding.ASCII.GetBytes($"{headerLines}\r\n");
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal("Request contains too many headers.", exception.Message);
            Assert.Equal(431, exception.StatusCode);
        }

        [Theory]
        [InlineData("Cookie: \r\n\r\n", 1)]
        [InlineData("Cookie:\r\n\r\n", 1)]
        [InlineData("Cookie: \r\nConnection: close\r\n\r\n", 2)]
        [InlineData("Cookie:\r\nConnection: close\r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie: \r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie:\r\n\r\n", 2)]
        public void EmptyHeaderValuesCanBeParsed(string rawHeaders, int numHeaders)
        {
            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            _socketInput.IncomingData(headerArray, 0, headerArray.Length);

            var success = _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);

            Assert.True(success);
            Assert.Equal(numHeaders, _frame.RequestHeaders.Count);

            // Assert TakeMessageHeaders consumed all the input
            var scan = _socketInput.ConsumingStart();
            Assert.True(scan.IsEnd);
        }

        [Fact]
        public void ResetResetsScheme()
        {
            _frame.Scheme = "https";

            // Act
            _frame.Reset();

            // Assert
            Assert.Equal("http", ((IFeatureCollection)_frame).Get<IHttpRequestFeature>().Scheme);
        }

        [Fact]
        public void ResetResetsHeaderLimits()
        {
            const string headerLine1 = "Header-1: value1\r\n";
            const string headerLine2 = "Header-2: value2\r\n";

            var options = new KestrelServerOptions();
            options.Limits.MaxRequestHeadersTotalSize = headerLine1.Length;
            options.Limits.MaxRequestHeaderCount = 1;
            _serviceContext.ServerOptions = options;

            var headerArray1 = Encoding.ASCII.GetBytes($"{headerLine1}\r\n");
            _socketInput.IncomingData(headerArray1, 0, headerArray1.Length);

            Assert.True(_frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value1", _frame.RequestHeaders["Header-1"]);

            _frame.Reset();

            var headerArray2 = Encoding.ASCII.GetBytes($"{headerLine2}\r\n");
            _socketInput.IncomingData(headerArray2, 0, headerArray1.Length);

            Assert.True(_frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value2", _frame.RequestHeaders["Header-2"]);
        }

        [Fact]
        public void ThrowsWhenStatusCodeIsSetAfterResponseStarted()
        {
            // Act
            _frame.Write(new ArraySegment<byte>(new byte[1]));

            // Assert
            Assert.True(_frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).StatusCode = 404);
        }

        [Fact]
        public void ThrowsWhenReasonPhraseIsSetAfterResponseStarted()
        {
            // Act
            _frame.Write(new ArraySegment<byte>(new byte[1]));

            // Assert
            Assert.True(_frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).ReasonPhrase = "Reason phrase");
        }

        [Fact]
        public void ThrowsWhenOnStartingIsSetAfterResponseStarted()
        {
            _frame.Write(new ArraySegment<byte>(new byte[1]));

            // Act/Assert
            Assert.True(_frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).OnStarting(_ => TaskCache.CompletedTask, null));
        }

        [Fact]
        public void InitializeHeadersResetsRequestHeaders()
        {
            // Arrange
            var originalRequestHeaders = _frame.RequestHeaders;
            _frame.RequestHeaders = new FrameRequestHeaders();

            // Act
            _frame.InitializeHeaders();

            // Assert
            Assert.Same(originalRequestHeaders, _frame.RequestHeaders);
        }

        [Fact]
        public void InitializeHeadersResetsResponseHeaders()
        {
            // Arrange
            var originalResponseHeaders = _frame.ResponseHeaders;
            _frame.ResponseHeaders = new FrameResponseHeaders();

            // Act
            _frame.InitializeHeaders();

            // Assert
            Assert.Same(originalResponseHeaders, _frame.ResponseHeaders);
        }

        [Fact]
        public void InitializeStreamsResetsStreams()
        {
            // Arrange
            var messageBody = MessageBody.For(HttpVersion.Http11, (FrameRequestHeaders)_frame.RequestHeaders, _frame);
            _frame.InitializeStreams(messageBody);

            var originalRequestBody = _frame.RequestBody;
            var originalResponseBody = _frame.ResponseBody;
            var originalDuplexStream = _frame.DuplexStream;
            _frame.RequestBody = new MemoryStream();
            _frame.ResponseBody = new MemoryStream();
            _frame.DuplexStream = new MemoryStream();

            // Act
            _frame.InitializeStreams(messageBody);

            // Assert
            Assert.Same(originalRequestBody, _frame.RequestBody);
            Assert.Same(originalResponseBody, _frame.ResponseBody);
            Assert.Same(originalDuplexStream, _frame.DuplexStream);
        }

        [Fact]
        public void TakeStartLineCallsConsumingCompleteWithFurthestExamined()
        {
            var requestLineBytes = Encoding.ASCII.GetBytes("GET / ");
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);
            _frame.TakeStartLine(_socketInput);
            Assert.False(_socketInput.IsCompleted);

            requestLineBytes = Encoding.ASCII.GetBytes("HTTP/1.1\r\n");
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);
            _frame.TakeStartLine(_socketInput);
            Assert.False(_socketInput.IsCompleted);
        }

        [Theory]
        [InlineData("", Frame.RequestLineStatus.Empty)]
        [InlineData("G", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GE", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET ", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET /", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / ", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / H", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HT", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTT", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTTP", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTTP/", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTTP/1", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTTP/1.", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTTP/1.1", Frame.RequestLineStatus.Incomplete)]
        [InlineData("GET / HTTP/1.1\r", Frame.RequestLineStatus.Incomplete)]
        public void TakeStartLineReturnsWhenGivenIncompleteRequestLines(string requestLine, Frame.RequestLineStatus expectedReturnValue)
        {
            var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);

            var returnValue = _frame.TakeStartLine(_socketInput);
            Assert.Equal(expectedReturnValue, returnValue);
        }

        [Fact]
        public void TakeStartLineStartsRequestHeadersTimeoutOnFirstByteAvailable()
        {
            var connectionControl = new Mock<IConnectionControl>();
            _connectionContext.ConnectionControl = connectionControl.Object;

            var requestLineBytes = Encoding.ASCII.GetBytes("G");
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);

            _frame.TakeStartLine(_socketInput);
            var expectedRequestHeadersTimeout = (long)_serviceContext.ServerOptions.Limits.RequestHeadersTimeout.TotalMilliseconds;
            connectionControl.Verify(cc => cc.ResetTimeout(expectedRequestHeadersTimeout, TimeoutAction.SendTimeoutResponse));
        }

        [Fact]
        public void TakeStartLineDoesNotStartRequestHeadersTimeoutIfNoDataAvailable()
        {
            var connectionControl = new Mock<IConnectionControl>();
            _connectionContext.ConnectionControl = connectionControl.Object;

            _frame.TakeStartLine(_socketInput);
            connectionControl.Verify(cc => cc.ResetTimeout(It.IsAny<long>(), It.IsAny<TimeoutAction>()), Times.Never);
        }

        [Fact]
        public void TakeStartLineThrowsWhenTooLong()
        {
            _serviceContext.ServerOptions.Limits.MaxRequestLineSize = "GET / HTTP/1.1\r\n".Length;

            var requestLineBytes = Encoding.ASCII.GetBytes("GET /a HTTP/1.1\r\n");
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(_socketInput));
            Assert.Equal("Request line too long.", exception.Message);
            Assert.Equal(414, exception.StatusCode);
        }

        [Theory]
        [InlineData("GET/HTTP/1.1\r\n", "Invalid request line: GET/HTTP/1.1<0x0D><0x0A>")]
        [InlineData(" / HTTP/1.1\r\n", "Invalid request line:  / HTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET? / HTTP/1.1\r\n", "Invalid request line: GET? / HTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET /HTTP/1.1\r\n", "Invalid request line: GET /HTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET /a?b=cHTTP/1.1\r\n", "Invalid request line: GET /a?b=cHTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET /a%20bHTTP/1.1\r\n", "Invalid request line: GET /a%20bHTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET /a%20b?c=dHTTP/1.1\r\n", "Invalid request line: GET /a%20b?c=dHTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET  HTTP/1.1\r\n", "Invalid request line: GET  HTTP/1.1<0x0D><0x0A>")]
        [InlineData("GET / HTTP/1.1\n", "Invalid request line: GET / HTTP/1.1<0x0A>")]
        [InlineData("GET / \r\n", "Invalid request line: GET / <0x0D><0x0A>")]
        [InlineData("GET / HTTP/1.1\ra\n", "Invalid request line: GET / HTTP/1.1<0x0D>a<0x0A>")]
        public void TakeStartLineThrowsWhenInvalid(string requestLine, string expectedExceptionMessage)
        {
            var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(_socketInput));
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Fact]
        public void TakeStartLineThrowsOnUnsupportedHttpVersion()
        {
            var requestLineBytes = Encoding.ASCII.GetBytes("GET / HTTP/1.2\r\n");
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(_socketInput));
            Assert.Equal("Unrecognized HTTP version: HTTP/1.2", exception.Message);
            Assert.Equal(505, exception.StatusCode);
        }

        [Fact]
        public void TakeStartLineThrowsOnUnsupportedHttpVersionLongerThanEightCharacters()
        {
            var requestLineBytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1ab\r\n");
            _socketInput.IncomingData(requestLineBytes, 0, requestLineBytes.Length);

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(_socketInput));
            Assert.Equal("Unrecognized HTTP version: HTTP/1.1a...", exception.Message);
            Assert.Equal(505, exception.StatusCode);
        }

        [Fact]
        public void TakeMessageHeadersCallsConsumingCompleteWithFurthestExamined()
        {
            var headersBytes = Encoding.ASCII.GetBytes("Header: ");
            _socketInput.IncomingData(headersBytes, 0, headersBytes.Length);
            _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);
            Assert.False(_socketInput.IsCompleted);

            headersBytes = Encoding.ASCII.GetBytes("value\r\n");
            _socketInput.IncomingData(headersBytes, 0, headersBytes.Length);
            _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);
            Assert.False(_socketInput.IsCompleted);

            headersBytes = Encoding.ASCII.GetBytes("\r\n");
            _socketInput.IncomingData(headersBytes, 0, headersBytes.Length);
            _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders);
            Assert.False(_socketInput.IsCompleted);
        }

        [Theory]
        [InlineData("\r")]
        [InlineData("H")]
        [InlineData("He")]
        [InlineData("Hea")]
        [InlineData("Head")]
        [InlineData("Heade")]
        [InlineData("Header")]
        [InlineData("Header:")]
        [InlineData("Header: ")]
        [InlineData("Header: v")]
        [InlineData("Header: va")]
        [InlineData("Header: val")]
        [InlineData("Header: valu")]
        [InlineData("Header: value")]
        [InlineData("Header: value\r")]
        [InlineData("Header: value\r\n")]
        [InlineData("Header: value\r\n\r")]
        public void TakeMessageHeadersReturnsWhenGivenIncompleteHeaders(string headers)
        {
            var headerBytes = Encoding.ASCII.GetBytes(headers);
            _socketInput.IncomingData(headerBytes, 0, headerBytes.Length);

            Assert.Equal(false, _frame.TakeMessageHeaders(_socketInput, (FrameRequestHeaders)_frame.RequestHeaders));
        }

        [Fact]
        public void RequestProcessingAsyncEnablesKeepAliveTimeout()
        {
            var connectionControl = new Mock<IConnectionControl>();
            _connectionContext.ConnectionControl = connectionControl.Object;

            var requestProcessingTask = _frame.RequestProcessingAsync();

            var expectedKeepAliveTimeout = (long)_serviceContext.ServerOptions.Limits.KeepAliveTimeout.TotalMilliseconds;
            connectionControl.Verify(cc => cc.SetTimeout(expectedKeepAliveTimeout, TimeoutAction.CloseConnection));

            _frame.StopAsync();
            _socketInput.IncomingFin();

            requestProcessingTask.Wait();
        }

        [Fact]
        public void WriteThrowsForNonBodyResponse()
        {
            // Arrange
            ((IHttpResponseFeature)_frame).StatusCode = 304;

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _frame.Write(new ArraySegment<byte>(new byte[1])));
        }

        [Fact]
        public async Task WriteAsyncThrowsForNonBodyResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpResponseFeature)_frame).StatusCode = 304;

            // Act/Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _frame.WriteAsync(new ArraySegment<byte>(new byte[1]), default(CancellationToken)));
        }

        [Fact]
        public void WriteDoesNotThrowForHeadResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpRequestFeature)_frame).Method = "HEAD";

            // Act/Assert
            _frame.Write(new ArraySegment<byte>(new byte[1]));
        }

        [Fact]
        public async Task WriteAsyncDoesNotThrowForHeadResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpRequestFeature)_frame).Method = "HEAD";

            // Act/Assert
            await _frame.WriteAsync(new ArraySegment<byte>(new byte[1]), default(CancellationToken));
        }

        [Fact]
        public void ManuallySettingTransferEncodingThrowsForHeadResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpRequestFeature)_frame).Method = "HEAD";

            // Act
            _frame.ResponseHeaders.Add("Transfer-Encoding", "chunked");

            // Assert
            Assert.Throws<InvalidOperationException>(() => _frame.Flush());
        }

        [Fact]
        public void ManuallySettingTransferEncodingThrowsForNoBodyResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpResponseFeature)_frame).StatusCode = 304;

            // Act
            _frame.ResponseHeaders.Add("Transfer-Encoding", "chunked");

            // Assert
            Assert.Throws<InvalidOperationException>(() => _frame.Flush());
        }

        [Fact]
        public async Task RequestProcessingTaskIsUnwrapped()
        {
            _frame.Start();

            var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
            _socketInput.IncomingData(data, 0, data.Length);

            var requestProcessingTask = _frame.StopAsync();
            Assert.IsNotType(typeof(Task<Task>), requestProcessingTask);

            await requestProcessingTask.TimeoutAfter(TimeSpan.FromSeconds(10));
            _socketInput.IncomingFin();
        }
    }
}
