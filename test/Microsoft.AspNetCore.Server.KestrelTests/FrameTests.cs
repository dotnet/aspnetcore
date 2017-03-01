// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameTests : IDisposable
    {
        private readonly IPipe _socketInput;
        private readonly TestFrame<object> _frame;
        private readonly ServiceContext _serviceContext;
        private readonly ConnectionContext _connectionContext;
        private readonly PipeFactory _pipelineFactory;
        private ReadCursor _consumed;
        private ReadCursor _examined;

        private class TestFrame<TContext> : Frame<TContext>
        {
            public TestFrame(IHttpApplication<TContext> application, ConnectionContext context)
            : base(application, context)
            {
            }

            public Task ProduceEndAsync()
            {
                return ProduceEnd();
            }
        }

        public FrameTests()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            _pipelineFactory = new PipeFactory();
            _socketInput = _pipelineFactory.Create();

            _serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                HttpParserFactory = frame => new KestrelHttpParser(trace),
                Log = trace
            };
            var listenerContext = new ListenerContext(_serviceContext)
            {
                ListenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 5000))
            };
            _connectionContext = new ConnectionContext(listenerContext)
            {
                Input = _socketInput,
                Output = new MockSocketOutput(),
                ConnectionControl = Mock.Of<IConnectionControl>()
            };

            _frame = new TestFrame<object>(application: null, context: _connectionContext);
            _frame.Reset();
            _frame.InitializeHeaders();
        }

        public void Dispose()
        {
            _socketInput.Reader.Complete();
            _socketInput.Writer.Complete();
            _pipelineFactory.Dispose();
        }

        [Fact]
        public async Task CanReadHeaderValueWithoutLeadingWhitespace()
        {
            _frame.InitializeHeaders();

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes("Header:value\r\n\r\n"));

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var success = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value", _frame.RequestHeaders["Header"]);
            Assert.Equal(readableBuffer.End, _consumed);
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
        public async Task LeadingWhitespaceIsNotIncludedInHeaderValue(string rawHeaders)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(rawHeaders));

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var success = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value", _frame.RequestHeaders["Header"]);
            Assert.Equal(readableBuffer.End, _consumed);
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
        public async Task TrailingWhitespaceIsNotIncludedInHeaderValue(string rawHeaders)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(rawHeaders));

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var success = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value", _frame.RequestHeaders["Header"]);
            Assert.Equal(readableBuffer.End, _consumed);
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
        public async Task WhitespaceWithinHeaderValueIsPreserved(string rawHeaders, string expectedValue)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(rawHeaders));

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var success = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(success);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal(expectedValue, _frame.RequestHeaders["Header"]);
            Assert.Equal(readableBuffer.End, _consumed);
        }

        [Theory]
        [MemberData(nameof(InvalidRequestHeaderData))]
        public async Task TakeMessageHeadersThrowsOnInvalidRequestHeaders(string rawHeaders, string expectedExceptionMessage)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(rawHeaders));
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task TakeMessageHeadersThrowsOnHeaderValueWithLineFolding_CharacterNotAvailableOnFirstAttempt()
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes("Header-1: value1\r\n"));

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            Assert.False(_frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(" "));

            readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal("Header value line folding not supported.", exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task TakeMessageHeadersThrowsWhenHeadersExceedTotalSizeLimit()
        {
            const string headerLine = "Header: value\r\n";
            _serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize = headerLine.Length - 1;
            _frame.Reset();

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine}\r\n"));
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal("Request headers too long.", exception.Message);
            Assert.Equal(StatusCodes.Status431RequestHeaderFieldsTooLarge, exception.StatusCode);
        }

        [Fact]
        public async Task TakeMessageHeadersThrowsWhenHeadersExceedCountLimit()
        {
            const string headerLines = "Header-1: value1\r\nHeader-2: value2\r\n";
            _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = 1;

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLines}\r\n"));
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal("Request contains too many headers.", exception.Message);
            Assert.Equal(StatusCodes.Status431RequestHeaderFieldsTooLarge, exception.StatusCode);
        }

        [Theory]
        [InlineData("Cookie: \r\n\r\n", 1)]
        [InlineData("Cookie:\r\n\r\n", 1)]
        [InlineData("Cookie: \r\nConnection: close\r\n\r\n", 2)]
        [InlineData("Cookie:\r\nConnection: close\r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie: \r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie:\r\n\r\n", 2)]
        public async Task EmptyHeaderValuesCanBeParsed(string rawHeaders, int numHeaders)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(rawHeaders));
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var success = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(success);
            Assert.Equal(numHeaders, _frame.RequestHeaders.Count);
            Assert.Equal(readableBuffer.End, _consumed);
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
        public async Task ResetResetsHeaderLimits()
        {
            const string headerLine1 = "Header-1: value1\r\n";
            const string headerLine2 = "Header-2: value2\r\n";

            var options = new KestrelServerOptions();
            options.Limits.MaxRequestHeadersTotalSize = headerLine1.Length;
            options.Limits.MaxRequestHeaderCount = 1;
            _serviceContext.ServerOptions = options;

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine1}\r\n"));
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var takeMessageHeaders = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(takeMessageHeaders);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value1", _frame.RequestHeaders["Header-1"]);

            _frame.Reset();

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine2}\r\n"));
            readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            takeMessageHeaders = _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(takeMessageHeaders);
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
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status404NotFound);
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
            var messageBody = MessageBody.For(Kestrel.Internal.Http.HttpVersion.Http11, (FrameRequestHeaders)_frame.RequestHeaders, _frame);
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

        [Theory]
        [MemberData(nameof(ValidRequestLineData))]
        public async Task TakeStartLineSetsFrameProperties(
            string requestLine,
            string expectedMethod,
            string expectedPath,
            string expectedQueryString,
            string expectedHttpVersion)
        {
            var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
            await _socketInput.Writer.WriteAsync(requestLineBytes);
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var returnValue = _frame.TakeStartLine(readableBuffer, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.True(returnValue);
            Assert.Equal(expectedMethod, _frame.Method);
            Assert.Equal(expectedPath, _frame.Path);
            Assert.Equal(expectedQueryString, _frame.QueryString);
            Assert.Equal(expectedHttpVersion, _frame.HttpVersion);
        }

        [Fact]
        public async Task TakeStartLineCallsConsumingCompleteWithFurthestExamined()
        {
            var requestLineBytes = Encoding.ASCII.GetBytes("GET / ");
            await _socketInput.Writer.WriteAsync(requestLineBytes);
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            _frame.TakeStartLine(readableBuffer, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal(readableBuffer.Start, _consumed);
            Assert.Equal(readableBuffer.End, _examined);

            requestLineBytes = Encoding.ASCII.GetBytes("HTTP/1.1\r\n");
            await _socketInput.Writer.WriteAsync(requestLineBytes);
            readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            _frame.TakeStartLine(readableBuffer, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal(readableBuffer.End, _consumed);
            Assert.Equal(readableBuffer.End, _examined);
        }

        [Theory]
        [InlineData("G")]
        [InlineData("GE")]
        [InlineData("GET")]
        [InlineData("GET ")]
        [InlineData("GET /")]
        [InlineData("GET / ")]
        [InlineData("GET / H")]
        [InlineData("GET / HT")]
        [InlineData("GET / HTT")]
        [InlineData("GET / HTTP")]
        [InlineData("GET / HTTP/")]
        [InlineData("GET / HTTP/1")]
        [InlineData("GET / HTTP/1.")]
        [InlineData("GET / HTTP/1.1")]
        [InlineData("GET / HTTP/1.1\r")]
        public async Task TakeStartLineReturnsWhenGivenIncompleteRequestLines(string requestLine)
        {
            var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
            await _socketInput.Writer.WriteAsync(requestLineBytes);

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var returnValue = _frame.TakeStartLine(readableBuffer, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.False(returnValue);
        }

        [Fact]
        public async Task TakeStartLineStartsRequestHeadersTimeoutOnFirstByteAvailable()
        {
            var connectionControl = new Mock<IConnectionControl>();
            _connectionContext.ConnectionControl = connectionControl.Object;

            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes("G"));

            _frame.TakeStartLine((await _socketInput.Reader.ReadAsync()).Buffer, out _consumed, out _examined);
            _socketInput.Reader.Advance(_consumed, _examined);

            var expectedRequestHeadersTimeout = (long)_serviceContext.ServerOptions.Limits.RequestHeadersTimeout.TotalMilliseconds;
            connectionControl.Verify(cc => cc.ResetTimeout(expectedRequestHeadersTimeout, TimeoutAction.SendTimeoutResponse));
        }

        [Fact]
        public async Task TakeStartLineThrowsWhenTooLong()
        {
            _serviceContext.ServerOptions.Limits.MaxRequestLineSize = "GET / HTTP/1.1\r\n".Length;

            var requestLineBytes = Encoding.ASCII.GetBytes("GET /a HTTP/1.1\r\n");
            await _socketInput.Writer.WriteAsync(requestLineBytes);

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal("Request line too long.", exception.Message);
            Assert.Equal(StatusCodes.Status414UriTooLong, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(InvalidRequestLineData))]
        public async Task TakeStartLineThrowsOnInvalidRequestLine(string requestLine, Type expectedExceptionType, string expectedExceptionMessage)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws(expectedExceptionType, () =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal(expectedExceptionMessage, exception.Message);

            if (expectedExceptionType == typeof(BadHttpRequestException))
            {
                Assert.Equal(StatusCodes.Status400BadRequest, (exception as BadHttpRequestException).StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(UnrecognizedHttpVersionData))]
        public async Task TakeStartLineThrowsOnUnrecognizedHttpVersion(string httpVersion)
        {
            await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes($"GET / {httpVersion}\r\n"));

            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _socketInput.Reader.Advance(_consumed, _examined);

            Assert.Equal($"Unrecognized HTTP version: {httpVersion}", exception.Message);
            Assert.Equal(StatusCodes.Status505HttpVersionNotsupported, exception.StatusCode);
        }

        [Fact]
        public async Task TakeMessageHeadersCallsConsumingCompleteWithFurthestExamined()
        {
            foreach (var rawHeader in new[] { "Header: ", "value\r\n", "\r\n" })
            {
                await _socketInput.Writer.WriteAsync(Encoding.ASCII.GetBytes(rawHeader));

                var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;
                _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out _consumed, out _examined);
                _socketInput.Reader.Advance(_consumed, _examined);
                Assert.Equal(readableBuffer.End, _examined);
            }
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
        public async Task TakeMessageHeadersReturnsWhenGivenIncompleteHeaders(string headers)
        {
            var headerBytes = Encoding.ASCII.GetBytes(headers);
            await _socketInput.Writer.WriteAsync(headerBytes);

            ReadCursor consumed;
            ReadCursor examined;
            var readableBuffer = (await _socketInput.Reader.ReadAsync()).Buffer;

            Assert.Equal(false, _frame.TakeMessageHeaders(readableBuffer, (FrameRequestHeaders)_frame.RequestHeaders, out consumed, out examined));
            _socketInput.Reader.Advance(consumed, examined);
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
            _socketInput.Writer.Complete();

            requestProcessingTask.Wait();
        }

        [Fact]
        public void WriteThrowsForNonBodyResponse()
        {
            // Arrange
            ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status304NotModified;

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _frame.Write(new ArraySegment<byte>(new byte[1])));
        }

        [Fact]
        public async Task WriteAsyncThrowsForNonBodyResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status304NotModified;

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
            ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status304NotModified;

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
            await _socketInput.Writer.WriteAsync(data);

            var requestProcessingTask = _frame.StopAsync();
            Assert.IsNotType(typeof(Task<Task>), requestProcessingTask);

            await requestProcessingTask.TimeoutAfter(TimeSpan.FromSeconds(10));
            _socketInput.Writer.Complete();
        }

        [Fact]
        public void RequestAbortedTokenIsResetBeforeLastWriteWithContentLength()
        {
            _frame.ResponseHeaders["Content-Length"] = "12";

            // Need to compare WaitHandle ref since CancellationToken is struct
            var original = _frame.RequestAborted.WaitHandle;

            foreach (var ch in "hello, worl")
            {
                _frame.Write(new ArraySegment<byte>(new[] { (byte)ch }));
                Assert.Same(original, _frame.RequestAborted.WaitHandle);
            }

            _frame.Write(new ArraySegment<byte>(new[] { (byte)'d' }));
            Assert.NotSame(original, _frame.RequestAborted.WaitHandle);
        }

        [Fact]
        public async Task RequestAbortedTokenIsResetBeforeLastWriteAsyncWithContentLength()
        {
            _frame.ResponseHeaders["Content-Length"] = "12";

            // Need to compare WaitHandle ref since CancellationToken is struct
            var original = _frame.RequestAborted.WaitHandle;

            foreach (var ch in "hello, worl")
            {
                await _frame.WriteAsync(new ArraySegment<byte>(new[] { (byte)ch }), default(CancellationToken));
                Assert.Same(original, _frame.RequestAborted.WaitHandle);
            }

            await _frame.WriteAsync(new ArraySegment<byte>(new[] { (byte)'d' }), default(CancellationToken));
            Assert.NotSame(original, _frame.RequestAborted.WaitHandle);
        }

        [Fact]
        public async Task RequestAbortedTokenIsResetBeforeLastWriteAsyncAwaitedWithContentLength()
        {
            _frame.ResponseHeaders["Content-Length"] = "12";

            // Need to compare WaitHandle ref since CancellationToken is struct
            var original = _frame.RequestAborted.WaitHandle;

            foreach (var ch in "hello, worl")
            {
                await _frame.WriteAsyncAwaited(new ArraySegment<byte>(new[] { (byte)ch }), default(CancellationToken));
                Assert.Same(original, _frame.RequestAborted.WaitHandle);
            }

            await _frame.WriteAsyncAwaited(new ArraySegment<byte>(new[] { (byte)'d' }), default(CancellationToken));
            Assert.NotSame(original, _frame.RequestAborted.WaitHandle);
        }

        [Fact]
        public async Task RequestAbortedTokenIsResetBeforeLastWriteWithChunkedEncoding()
        {
            // Need to compare WaitHandle ref since CancellationToken is struct
            var original = _frame.RequestAborted.WaitHandle;

            _frame.HttpVersion = "HTTP/1.1";
            await _frame.WriteAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello, world")), default(CancellationToken));
            Assert.Same(original, _frame.RequestAborted.WaitHandle);

            await _frame.ProduceEndAsync();
            Assert.NotSame(original, _frame.RequestAborted.WaitHandle);
        }

        public static IEnumerable<object> ValidRequestLineData => HttpParsingData.ValidRequestLineData;

        public static IEnumerable<object> InvalidRequestLineData => HttpParsingData.InvalidRequestLineData;

        public static TheoryData<string> UnrecognizedHttpVersionData => HttpParsingData.UnrecognizedHttpVersionData;

        public static IEnumerable<object[]> InvalidRequestHeaderData => HttpParsingData.InvalidRequestHeaderData;
    }
}
