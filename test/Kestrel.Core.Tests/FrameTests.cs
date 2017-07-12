// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class FrameTests : IDisposable
    {
        private readonly IPipe _input;
        private readonly TestFrame<object> _frame;
        private readonly ServiceContext _serviceContext;
        private readonly FrameContext _frameContext;
        private readonly PipeFactory _pipelineFactory;
        private ReadCursor _consumed;
        private ReadCursor _examined;
        private Mock<ITimeoutControl> _timeoutControl;

        private class TestFrame<TContext> : Frame<TContext>
        {
            public TestFrame(IHttpApplication<TContext> application, FrameContext context)
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
            _pipelineFactory = new PipeFactory();
            _input = _pipelineFactory.Create();
            var output = _pipelineFactory.Create();

            _serviceContext = new TestServiceContext();
            _timeoutControl = new Mock<ITimeoutControl>();
            _frameContext = new FrameContext
            {
                ServiceContext = _serviceContext,
                ConnectionInformation = new MockConnectionInformation
                {
                    PipeFactory = _pipelineFactory
                },
                TimeoutControl = _timeoutControl.Object,
                Input = _input.Reader,
                Output = output
            };

            _frame = new TestFrame<object>(application: null, context: _frameContext);
            _frame.Reset();
        }

        public void Dispose()
        {
            _input.Reader.Complete();
            _input.Writer.Complete();
            _pipelineFactory.Dispose();
        }

        [Fact]
        public async Task TakeMessageHeadersThrowsWhenHeadersExceedTotalSizeLimit()
        {
            const string headerLine = "Header: value\r\n";
            _serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize = headerLine.Length - 1;
            _frame.Reset();

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine}\r\n"));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, exception.Message);
            Assert.Equal(StatusCodes.Status431RequestHeaderFieldsTooLarge, exception.StatusCode);
        }

        [Fact]
        public async Task TakeMessageHeadersThrowsWhenHeadersExceedCountLimit()
        {
            const string headerLines = "Header-1: value1\r\nHeader-2: value2\r\n";
            _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = 1;

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLines}\r\n"));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeMessageHeaders(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.BadRequest_TooManyHeaders, exception.Message);
            Assert.Equal(StatusCodes.Status431RequestHeaderFieldsTooLarge, exception.StatusCode);
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
        public void ResetResetsTraceIdentifier()
        {
            _frame.TraceIdentifier = "xyz";

            _frame.Reset();

            var nextId = ((IFeatureCollection)_frame).Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
            Assert.NotEqual("xyz", nextId);

            _frame.Reset();
            var secondId = ((IFeatureCollection)_frame).Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
            Assert.NotEqual(nextId, secondId);
        }

        [Fact]
        public void ResetResetsMinRequestBodyDataRate()
        {
            _frame.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 1, gracePeriod: TimeSpan.MaxValue);

            _frame.Reset();

            Assert.Same(_serviceContext.ServerOptions.Limits.MinRequestBodyDataRate, _frame.MinRequestBodyDataRate);
        }

        [Fact]
        public void ResetResetsMinResponseDataRate()
        {
            _frame.MinResponseDataRate = new MinDataRate(bytesPerSecond: 1, gracePeriod: TimeSpan.MaxValue);

            _frame.Reset();

            Assert.Same(_serviceContext.ServerOptions.Limits.MinResponseDataRate, _frame.MinResponseDataRate);
        }

        [Fact]
        public void TraceIdentifierCountsRequestsPerFrame()
        {
            var connectionId = _frameContext.ConnectionId;
            var feature = ((IFeatureCollection)_frame).Get<IHttpRequestIdentifierFeature>();
            // Reset() is called once in the test ctor
            var count = 1;
            void Reset()
            {
                _frame.Reset();
                count++;
            }

            var nextId = feature.TraceIdentifier;
            Assert.Equal($"{connectionId}:00000001", nextId);

            Reset();
            var secondId = feature.TraceIdentifier;
            Assert.Equal($"{connectionId}:00000002", secondId);

            var big = 1_000_000;
            while (big-- > 0) Reset();
            Assert.Equal($"{connectionId}:{count:X8}", feature.TraceIdentifier);
        }

        [Fact]
        public void TraceIdentifierGeneratesWhenNull()
        {
            _frame.TraceIdentifier = null;
            var id = _frame.TraceIdentifier;
            Assert.NotNull(id);
            Assert.Equal(id, _frame.TraceIdentifier);

            _frame.Reset();
            Assert.NotEqual(id, _frame.TraceIdentifier);
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

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine1}\r\n"));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var takeMessageHeaders = _frame.TakeMessageHeaders(readableBuffer, out _consumed, out _examined);
            _input.Reader.Advance(_consumed, _examined);

            Assert.True(takeMessageHeaders);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value1", _frame.RequestHeaders["Header-1"]);

            _frame.Reset();

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine2}\r\n"));
            readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            takeMessageHeaders = _frame.TakeMessageHeaders(readableBuffer, out _consumed, out _examined);
            _input.Reader.Advance(_consumed, _examined);

            Assert.True(takeMessageHeaders);
            Assert.Equal(1, _frame.RequestHeaders.Count);
            Assert.Equal("value2", _frame.RequestHeaders["Header-2"]);
        }

        [Fact]
        public async Task ThrowsWhenStatusCodeIsSetAfterResponseStarted()
        {
            // Act
            await _frame.WriteAsync(new ArraySegment<byte>(new byte[1]));

            // Assert
            Assert.True(_frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ThrowsWhenReasonPhraseIsSetAfterResponseStarted()
        {
            // Act
            await _frame.WriteAsync(new ArraySegment<byte>(new byte[1]));

            // Assert
            Assert.True(_frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).ReasonPhrase = "Reason phrase");
        }

        [Fact]
        public async Task ThrowsWhenOnStartingIsSetAfterResponseStarted()
        {
            await _frame.WriteAsync(new ArraySegment<byte>(new byte[1]));

            // Act/Assert
            Assert.True(_frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_frame).OnStarting(_ => Task.CompletedTask, null));
        }

        [Theory]
        [MemberData(nameof(MinDataRateData))]
        public void ConfiguringIHttpMinRequestBodyDataRateFeatureSetsMinRequestBodyDataRate(MinDataRate minDataRate)
        {
            ((IFeatureCollection)_frame).Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = minDataRate;

            Assert.Same(minDataRate, _frame.MinRequestBodyDataRate);
        }

        [Theory]
        [MemberData(nameof(MinDataRateData))]
        public void ConfiguringIHttpMinResponseDataRateFeatureSetsMinResponseDataRate(MinDataRate minDataRate)
        {
            ((IFeatureCollection)_frame).Get<IHttpMinResponseDataRateFeature>().MinDataRate = minDataRate;

            Assert.Same(minDataRate, _frame.MinResponseDataRate);
        }

        [Fact]
        public void ResetResetsRequestHeaders()
        {
            // Arrange
            var originalRequestHeaders = _frame.RequestHeaders;
            _frame.RequestHeaders = new FrameRequestHeaders();

            // Act
            _frame.Reset();

            // Assert
            Assert.Same(originalRequestHeaders, _frame.RequestHeaders);
        }

        [Fact]
        public void ResetResetsResponseHeaders()
        {
            // Arrange
            var originalResponseHeaders = _frame.ResponseHeaders;
            _frame.ResponseHeaders = new FrameResponseHeaders();

            // Act
            _frame.Reset();

            // Assert
            Assert.Same(originalResponseHeaders, _frame.ResponseHeaders);
        }

        [Fact]
        public void InitializeStreamsResetsStreams()
        {
            // Arrange
            var messageBody = MessageBody.For(Kestrel.Core.Internal.Http.HttpVersion.Http11, (FrameRequestHeaders)_frame.RequestHeaders, _frame);
            _frame.InitializeStreams(messageBody);

            var originalRequestBody = _frame.RequestBody;
            var originalResponseBody = _frame.ResponseBody;
            _frame.RequestBody = new MemoryStream();
            _frame.ResponseBody = new MemoryStream();

            // Act
            _frame.InitializeStreams(messageBody);

            // Assert
            Assert.Same(originalRequestBody, _frame.RequestBody);
            Assert.Same(originalResponseBody, _frame.ResponseBody);
        }

        [Theory]
        [MemberData(nameof(RequestLineValidData))]
        public async Task TakeStartLineSetsFrameProperties(
            string requestLine,
            string expectedMethod,
            string expectedRawTarget,
            string expectedRawPath,
            string expectedDecodedPath,
            string expectedQueryString,
            string expectedHttpVersion)
        {
            var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
            await _input.Writer.WriteAsync(requestLineBytes);
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var returnValue = _frame.TakeStartLine(readableBuffer, out _consumed, out _examined);
            _input.Reader.Advance(_consumed, _examined);

            Assert.True(returnValue);
            Assert.Equal(expectedMethod, _frame.Method);
            Assert.Equal(expectedRawTarget, _frame.RawTarget);
            Assert.Equal(expectedDecodedPath, _frame.Path);
            Assert.Equal(expectedQueryString, _frame.QueryString);
            Assert.Equal(expectedHttpVersion, _frame.HttpVersion);
        }

        [Theory]
        [MemberData(nameof(RequestLineDotSegmentData))]
        public async Task TakeStartLineRemovesDotSegmentsFromTarget(
            string requestLine,
            string expectedRawTarget,
            string expectedDecodedPath,
            string expectedQueryString)
        {
            var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
            await _input.Writer.WriteAsync(requestLineBytes);
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var returnValue = _frame.TakeStartLine(readableBuffer, out _consumed, out _examined);
            _input.Reader.Advance(_consumed, _examined);

            Assert.True(returnValue);
            Assert.Equal(expectedRawTarget, _frame.RawTarget);
            Assert.Equal(expectedDecodedPath, _frame.Path);
            Assert.Equal(expectedQueryString, _frame.QueryString);
        }

        [Fact]
        public async Task ParseRequestStartsRequestHeadersTimeoutOnFirstByteAvailable()
        {
            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes("G"));

            _frame.ParseRequest((await _input.Reader.ReadAsync()).Buffer, out _consumed, out _examined);
            _input.Reader.Advance(_consumed, _examined);

            var expectedRequestHeadersTimeout = _serviceContext.ServerOptions.Limits.RequestHeadersTimeout.Ticks;
            _timeoutControl.Verify(cc => cc.ResetTimeout(expectedRequestHeadersTimeout, TimeoutAction.SendTimeoutResponse));
        }

        [Fact]
        public async Task TakeStartLineThrowsWhenTooLong()
        {
            _serviceContext.ServerOptions.Limits.MaxRequestLineSize = "GET / HTTP/1.1\r\n".Length;

            var requestLineBytes = Encoding.ASCII.GetBytes("GET /a HTTP/1.1\r\n");
            await _input.Writer.WriteAsync(requestLineBytes);

            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;
            var exception = Assert.Throws<BadHttpRequestException>(() => _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.BadRequest_RequestLineTooLong, exception.Message);
            Assert.Equal(StatusCodes.Status414UriTooLong, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(TargetWithEncodedNullCharData))]
        public async Task TakeStartLineThrowsOnEncodedNullCharInTarget(string target)
        {
            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\n"));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target), exception.Message);
        }

        [Theory]
        [MemberData(nameof(TargetWithNullCharData))]
        public async Task TakeStartLineThrowsOnNullCharInTarget(string target)
        {
            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\n"));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target.EscapeNonPrintable()), exception.Message);
        }

        [Theory]
        [MemberData(nameof(MethodWithNullCharData))]
        public async Task TakeStartLineThrowsOnNullCharInMethod(string method)
        {
            var requestLine = $"{method} / HTTP/1.1\r\n";

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(requestLine.EscapeNonPrintable()), exception.Message);
        }

        [Theory]
        [MemberData(nameof(QueryStringWithNullCharData))]
        public async Task TakeStartLineThrowsOnNullCharInQueryString(string queryString)
        {
            var target = $"/{queryString}";

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\n"));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target.EscapeNonPrintable()), exception.Message);
        }

        [Theory]
        [MemberData(nameof(TargetInvalidData))]
        public async Task TakeStartLineThrowsWhenRequestTargetIsInvalid(string method, string target)
        {
            var requestLine = $"{method} {target} HTTP/1.1\r\n";

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target.EscapeNonPrintable()), exception.Message);
        }

        [Theory]
        [MemberData(nameof(MethodNotAllowedTargetData))]
        public async Task TakeStartLineThrowsWhenMethodNotAllowed(string requestLine, HttpMethod allowedMethod)
        {
            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
            var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

            var exception = Assert.Throws<BadHttpRequestException>(() =>
                _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
            _input.Reader.Advance(_consumed, _examined);

            Assert.Equal(405, exception.StatusCode);
            Assert.Equal(CoreStrings.BadRequest_MethodNotAllowed, exception.Message);
            Assert.Equal(HttpUtilities.MethodToString(allowedMethod), exception.AllowedHeader);
        }

        [Fact]
        public void ProcessRequestsAsyncEnablesKeepAliveTimeout()
        {
            var requestProcessingTask = _frame.ProcessRequestsAsync();

            var expectedKeepAliveTimeout = _serviceContext.ServerOptions.Limits.KeepAliveTimeout.Ticks;
            _timeoutControl.Verify(cc => cc.SetTimeout(expectedKeepAliveTimeout, TimeoutAction.CloseConnection));

            _frame.Stop();
            _input.Writer.Complete();

            requestProcessingTask.Wait();
        }

        [Fact]
        public async Task WriteThrowsForNonBodyResponse()
        {
            // Arrange
            ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status304NotModified;

            // Act/Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _frame.WriteAsync(new ArraySegment<byte>(new byte[1])));
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
        public async Task WriteDoesNotThrowForHeadResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpRequestFeature)_frame).Method = "HEAD";

            // Act/Assert
            await _frame.WriteAsync(new ArraySegment<byte>(new byte[1]));
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
        public async Task ManuallySettingTransferEncodingThrowsForHeadResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpRequestFeature)_frame).Method = "HEAD";

            // Act
            _frame.ResponseHeaders.Add("Transfer-Encoding", "chunked");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _frame.FlushAsync());
        }

        [Fact]
        public async Task ManuallySettingTransferEncodingThrowsForNoBodyResponse()
        {
            // Arrange
            _frame.HttpVersion = "HTTP/1.1";
            ((IHttpResponseFeature)_frame).StatusCode = StatusCodes.Status304NotModified;

            // Act
            _frame.ResponseHeaders.Add("Transfer-Encoding", "chunked");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _frame.FlushAsync());
        }

        [Fact]
        public async Task RequestProcessingTaskIsUnwrapped()
        {
            var requestProcessingTask = _frame.ProcessRequestsAsync();

            var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
            await _input.Writer.WriteAsync(data);

            _frame.Stop();
            Assert.IsNotType(typeof(Task<Task>), requestProcessingTask);

            await requestProcessingTask.TimeoutAfter(TimeSpan.FromSeconds(10));
            _input.Writer.Complete();
        }

        [Fact]
        public async Task RequestAbortedTokenIsResetBeforeLastWriteWithContentLength()
        {
            _frame.ResponseHeaders["Content-Length"] = "12";

            // Need to compare WaitHandle ref since CancellationToken is struct
            var original = _frame.RequestAborted.WaitHandle;

            foreach (var ch in "hello, worl")
            {
                await _frame.WriteAsync(new ArraySegment<byte>(new[] { (byte)ch }));
                Assert.Same(original, _frame.RequestAborted.WaitHandle);
            }

            await _frame.WriteAsync(new ArraySegment<byte>(new[] { (byte)'d' }));
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

        [Fact]
        public async Task ExceptionDetailNotIncludedWhenLogLevelInformationNotEnabled()
        {
            var previousLog = _serviceContext.Log;

            try
            {
                var mockTrace = new Mock<IKestrelTrace>();
                mockTrace
                    .Setup(trace => trace.IsEnabled(LogLevel.Information))
                    .Returns(false);

                _serviceContext.Log = mockTrace.Object;

                await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes($"GET /%00 HTTP/1.1\r\n"));
                var readableBuffer = (await _input.Reader.ReadAsync()).Buffer;

                var exception = Assert.Throws<BadHttpRequestException>(() =>
                    _frame.TakeStartLine(readableBuffer, out _consumed, out _examined));
                _input.Reader.Advance(_consumed, _examined);

                Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(string.Empty), exception.Message);
                Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
            }
            finally
            {
                _serviceContext.Log = previousLog;
            }
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        [InlineData(100, 100)]
        [InlineData(600, 100)]
        [InlineData(700, 1)]
        [InlineData(1, 700)]
        public async Task AcceptsHeadersAcrossSends(int header0Count, int header1Count)
        {
            _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = header0Count + header1Count;

            var headers0 = MakeHeaders(header0Count);
            var headers1 = MakeHeaders(header1Count, header0Count);

            var requestProcessingTask = _frame.ProcessRequestsAsync();

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n"));
            await WaitForCondition(TimeSpan.FromSeconds(1), () => _frame.RequestHeaders != null);
            Assert.Equal(0, _frame.RequestHeaders.Count);

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(headers0));
            await WaitForCondition(TimeSpan.FromSeconds(1), () => _frame.RequestHeaders.Count >= header0Count);
            Assert.Equal(header0Count, _frame.RequestHeaders.Count);

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(headers1));
            await WaitForCondition(TimeSpan.FromSeconds(1), () => _frame.RequestHeaders.Count >= header0Count + header1Count);
            Assert.Equal(header0Count + header1Count, _frame.RequestHeaders.Count);

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));
            Assert.Equal(header0Count + header1Count, _frame.RequestHeaders.Count);

            await requestProcessingTask.TimeoutAfter(TimeSpan.FromSeconds(10));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        [InlineData(100, 100)]
        [InlineData(600, 100)]
        [InlineData(700, 1)]
        [InlineData(1, 700)]
        public async Task KeepsSameHeaderCollectionAcrossSends(int header0Count, int header1Count)
        {
            _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = header0Count + header1Count;

            var headers0 = MakeHeaders(header0Count);
            var headers1 = MakeHeaders(header1Count, header0Count);

            var requestProcessingTask = _frame.ProcessRequestsAsync();

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n"));
            await WaitForCondition(TimeSpan.FromSeconds(1), () => _frame.RequestHeaders != null);
            Assert.Equal(0, _frame.RequestHeaders.Count);

            var newRequestHeaders = new RequestHeadersWrapper(_frame.RequestHeaders);
            _frame.RequestHeaders = newRequestHeaders;
            Assert.Same(newRequestHeaders, _frame.RequestHeaders);

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(headers0));
            await WaitForCondition(TimeSpan.FromSeconds(1), () => _frame.RequestHeaders.Count >= header0Count);
            Assert.Same(newRequestHeaders, _frame.RequestHeaders);
            Assert.Equal(header0Count, _frame.RequestHeaders.Count);

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes(headers1));
            await WaitForCondition(TimeSpan.FromSeconds(1), () => _frame.RequestHeaders.Count >= header0Count + header1Count);
            Assert.Same(newRequestHeaders, _frame.RequestHeaders);
            Assert.Equal(header0Count + header1Count, _frame.RequestHeaders.Count);

            await _input.Writer.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));
            Assert.Same(newRequestHeaders, _frame.RequestHeaders);
            Assert.Equal(header0Count + header1Count, _frame.RequestHeaders.Count);

            await requestProcessingTask.TimeoutAfter(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void ThrowsWhenMaxRequestBodySizeIsSetAfterReadingFromRequestBody()
        {
            // Act
            // This would normally be set by the MessageBody during the first read.
            _frame.HasStartedConsumingRequestBody = true;

            // Assert
            Assert.True(((IHttpMaxRequestBodySizeFeature)_frame).IsReadOnly);
            var ex = Assert.Throws<InvalidOperationException>(() => ((IHttpMaxRequestBodySizeFeature)_frame).MaxRequestBodySize = 1);
            Assert.Equal(CoreStrings.MaxRequestBodySizeCannotBeModifiedAfterRead, ex.Message);
        }

        [Fact]
        public void ThrowsWhenMaxRequestBodySizeIsSetToANegativeValue()
        {
            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((IHttpMaxRequestBodySizeFeature)_frame).MaxRequestBodySize = -1);
            Assert.StartsWith(CoreStrings.NonNegativeNumberOrNullRequired, ex.Message);
        }

        private static async Task WaitForCondition(TimeSpan timeout, Func<bool> condition)
        {
            const int MaxWaitLoop = 150;

            var delay = (int)Math.Ceiling(timeout.TotalMilliseconds / MaxWaitLoop);

            var waitLoop = 0;
            while (waitLoop < MaxWaitLoop && !condition())
            {
                // Wait for parsing condition to trigger
                await Task.Delay(delay);
                waitLoop++;
            }
        }

        private static string MakeHeaders(int count, int startAt = 0)
        {
            return string.Join("", Enumerable
                .Range(0, count)
                .Select(i => $"Header-{startAt + i}: value{startAt + i}\r\n"));
        }

        public static IEnumerable<object> RequestLineValidData => HttpParsingData.RequestLineValidData;

        public static IEnumerable<object> RequestLineDotSegmentData => HttpParsingData.RequestLineDotSegmentData;

        public static TheoryData<string> TargetWithEncodedNullCharData
        {
            get
            {
                var data = new TheoryData<string>();

                foreach (var target in HttpParsingData.TargetWithEncodedNullCharData)
                {
                    data.Add(target);
                }

                return data;
            }
        }

        public static TheoryData<string, string> TargetInvalidData
            => HttpParsingData.TargetInvalidData;

        public static TheoryData<string, HttpMethod> MethodNotAllowedTargetData
            => HttpParsingData.MethodNotAllowedRequestLine;

        public static TheoryData<string> TargetWithNullCharData
        {
            get
            {
                var data = new TheoryData<string>();

                foreach (var target in HttpParsingData.TargetWithNullCharData)
                {
                    data.Add(target);
                }

                return data;
            }
        }

        public static TheoryData<string> MethodWithNullCharData
        {
            get
            {
                var data = new TheoryData<string>();

                foreach (var target in HttpParsingData.MethodWithNullCharData)
                {
                    data.Add(target);
                }

                return data;
            }
        }

        public static TheoryData<string> QueryStringWithNullCharData
        {
            get
            {
                var data = new TheoryData<string>();

                foreach (var target in HttpParsingData.QueryStringWithNullCharData)
                {
                    data.Add(target);
                }

                return data;
            }
        }

        public static TheoryData<TimeSpan> RequestBodyTimeoutDataValid => new TheoryData<TimeSpan>
        {
            TimeSpan.FromTicks(1),
            TimeSpan.MaxValue,
            Timeout.InfiniteTimeSpan,
            TimeSpan.FromMilliseconds(-1) // Same as Timeout.InfiniteTimeSpan
        };

        public static TheoryData<TimeSpan> RequestBodyTimeoutDataInvalid => new TheoryData<TimeSpan>
        {
            TimeSpan.MinValue,
            TimeSpan.FromTicks(-1),
            TimeSpan.Zero
        };

        public static TheoryData<MinDataRate> MinDataRateData => new TheoryData<MinDataRate>
        {
            null,
            new MinDataRate(bytesPerSecond: 1, gracePeriod: TimeSpan.MaxValue)
        };

        private class RequestHeadersWrapper : IHeaderDictionary
        {
            IHeaderDictionary _innerHeaders;

            public RequestHeadersWrapper(IHeaderDictionary headers)
            {
                _innerHeaders = headers;
            }

            public StringValues this[string key] { get => _innerHeaders[key]; set => _innerHeaders[key] = value; }
            public long? ContentLength { get => _innerHeaders.ContentLength; set => _innerHeaders.ContentLength = value; }
            public ICollection<string> Keys => _innerHeaders.Keys;
            public ICollection<StringValues> Values => _innerHeaders.Values;
            public int Count => _innerHeaders.Count;
            public bool IsReadOnly => _innerHeaders.IsReadOnly;
            public void Add(string key, StringValues value) => _innerHeaders.Add(key, value);
            public void Add(KeyValuePair<string, StringValues> item) => _innerHeaders.Add(item);
            public void Clear() => _innerHeaders.Clear();
            public bool Contains(KeyValuePair<string, StringValues> item) => _innerHeaders.Contains(item);
            public bool ContainsKey(string key) => _innerHeaders.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) => _innerHeaders.CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _innerHeaders.GetEnumerator();
            public bool Remove(string key) => _innerHeaders.Remove(key);
            public bool Remove(KeyValuePair<string, StringValues> item) => _innerHeaders.Remove(item);
            public bool TryGetValue(string key, out StringValues value) => _innerHeaders.TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => _innerHeaders.GetEnumerator();
        }
    }
}
