// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameTests
    {
        [Fact]
        public void CanReadHeaderValueWithoutLeadingWhitespace()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes("Header:value\r\n\r\n");
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var success = frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders);

                Assert.True(success);
                Assert.Equal(1, frame.RequestHeaders.Count);
                Assert.Equal("value", frame.RequestHeaders["Header"]);

                // Assert TakeMessageHeaders consumed all the input
                var scan = socketInput.ConsumingStart();
                Assert.True(scan.IsEnd);
            }
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
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var success = frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders);

                Assert.True(success);
                Assert.Equal(1, frame.RequestHeaders.Count);
                Assert.Equal("value", frame.RequestHeaders["Header"]);

                // Assert TakeMessageHeaders consumed all the input
                var scan = socketInput.ConsumingStart();
                Assert.True(scan.IsEnd);
            }
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
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var success = frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders);

                Assert.True(success);
                Assert.Equal(1, frame.RequestHeaders.Count);
                Assert.Equal("value", frame.RequestHeaders["Header"]);

                // Assert TakeMessageHeaders consumed all the input
                var scan = socketInput.ConsumingStart();
                Assert.True(scan.IsEnd);
            }
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
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var success = frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders);

                Assert.True(success);
                Assert.Equal(1, frame.RequestHeaders.Count);
                Assert.Equal(expectedValue, frame.RequestHeaders["Header"]);

                // Assert TakeMessageHeaders consumed all the input
                var scan = socketInput.ConsumingStart();
                Assert.True(scan.IsEnd);
            }
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
        public void ThrowsOnHeaderValueWithLineFolding(string rawHeaders)
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Header value line folding not supported.", exception.Message);
            }
        }

        [Fact]
        public void ThrowsOnHeaderValueWithLineFolding_CharacterNotAvailableOnFirstAttempt()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes("Header-1: value1\r\n");
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                Assert.False(frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));

                socketInput.IncomingData(Encoding.ASCII.GetBytes(" "), 0, 1);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Header value line folding not supported.", exception.Message);
            }
        }

        [Theory]
        [InlineData("Header-1: value1\r\r\n")]
        [InlineData("Header-1: val\rue1\r\n")]
        [InlineData("Header-1: value1\rHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2: v\ralue2\r\n")]
        public void ThrowsOnHeaderValueContainingCR(string rawHeaders)
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Header value must not contain CR characters.", exception.Message);
            }
        }

        [Theory]
        [InlineData("Header-1 value1\r\n\r\n")]
        [InlineData("Header-1 value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("Header-1: value1\r\nHeader-2 value2\r\n\r\n")]
        public void ThrowsOnHeaderLineMissingColon(string rawHeaders)
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("No ':' character found in header line.", exception.Message);
            }
        }

        [Theory]
        [InlineData(" Header: value\r\n\r\n")]
        [InlineData("\tHeader: value\r\n\r\n")]
        [InlineData(" Header-1: value1\r\nHeader-2: value2\r\n\r\n")]
        [InlineData("\tHeader-1: value1\r\nHeader-2: value2\r\n\r\n")]
        public void ThrowsOnHeaderLineStartingWithWhitespace(string rawHeaders)
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Header line must not start with whitespace.", exception.Message);
            }
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
        public void ThrowsOnWhitespaceInHeaderName(string rawHeaders)
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Whitespace is not allowed in header name.", exception.Message);
            }
        }

        [Theory]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r\r")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r ")]
        [InlineData("Header-1: value1\r\nHeader-2: value2\r\n\r \n")]
        public void ThrowsOnHeadersNotEndingInCRLFLine(string rawHeaders)
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                    Log = trace
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Headers corrupted, invalid header sequence.", exception.Message);
            }
        }

        [Fact]
        public void ThrowsWhenHeadersExceedTotalSizeLimit()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                const string headerLine = "Header: value\r\n";

                var options = new KestrelServerOptions();
                options.Limits.MaxRequestHeadersTotalSize = headerLine.Length - 1;

                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = options,
                    Log = trace
                };

                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes($"{headerLine}\r\n");
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Request headers too long.", exception.Message);
            }
        }

        [Fact]
        public void ThrowsWhenHeadersExceedCountLimit()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                const string headerLines = "Header-1: value1\r\nHeader-2: value2\r\n";

                var options = new KestrelServerOptions();
                options.Limits.MaxRequestHeaderCount = 1;

                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = options,
                    Log = trace
                };

                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes($"{headerLines}\r\n");
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var exception = Assert.Throws<BadHttpRequestException>(() => frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal("Request contains too many headers.", exception.Message);
            }
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
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = new KestrelServerOptions(),
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var success = frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders);

                Assert.True(success);
                Assert.Equal(numHeaders, frame.RequestHeaders.Count);

                // Assert TakeMessageHeaders consumed all the input
                var scan = socketInput.ConsumingStart();
                Assert.True(scan.IsEnd);
            }
        }

        [Fact]
        public void ResetResetsScheme()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.Scheme = "https";

            // Act
            frame.Reset();

            // Assert
            Assert.Equal("http", ((IFeatureCollection)frame).Get<IHttpRequestFeature>().Scheme);
        }

        [Fact]
        public void ResetResetsHeaderLimits()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var pool = new MemoryPool())
            using (var socketInput = new SocketInput(pool, ltp))
            {
                const string headerLine1 = "Header-1: value1\r\n";
                const string headerLine2 = "Header-2: value2\r\n";

                var options = new KestrelServerOptions();
                options.Limits.MaxRequestHeadersTotalSize = headerLine1.Length;
                options.Limits.MaxRequestHeaderCount = 1;

                var connectionContext = new ConnectionContext()
                {
                    DateHeaderValueManager = new DateHeaderValueManager(),
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                    ServerOptions = options
                };

                var frame = new Frame<object>(application: null, context: connectionContext);
                frame.Reset();
                frame.InitializeHeaders();

                var headerArray1 = Encoding.ASCII.GetBytes($"{headerLine1}\r\n");
                socketInput.IncomingData(headerArray1, 0, headerArray1.Length);

                Assert.True(frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal(1, frame.RequestHeaders.Count);
                Assert.Equal("value1", frame.RequestHeaders["Header-1"]);

                frame.Reset();

                var headerArray2 = Encoding.ASCII.GetBytes($"{headerLine2}\r\n");
                socketInput.IncomingData(headerArray2, 0, headerArray1.Length);

                Assert.True(frame.TakeMessageHeaders(socketInput, (FrameRequestHeaders)frame.RequestHeaders));
                Assert.Equal(1, frame.RequestHeaders.Count);
                Assert.Equal("value2", frame.RequestHeaders["Header-2"]);
            }
        }

        [Fact]
        public void ThrowsWhenStatusCodeIsSetAfterResponseStarted()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
                SocketOutput = new MockSocketOuptut()
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

            // Act
            frame.Write(new ArraySegment<byte>(new byte[1]));

            // Assert
            Assert.True(frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)frame).StatusCode = 404);
        }

        [Fact]
        public void ThrowsWhenReasonPhraseIsSetAfterResponseStarted()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
                SocketOutput = new MockSocketOuptut()
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

            // Act
            frame.Write(new ArraySegment<byte>(new byte[1]));

            // Assert
            Assert.True(frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)frame).ReasonPhrase = "Reason phrase");
        }

        [Fact]
        public void ThrowsWhenOnStartingIsSetAfterResponseStarted()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
                SocketOutput = new MockSocketOuptut()
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();
            frame.Write(new ArraySegment<byte>(new byte[1]));

            // Act/Assert
            Assert.True(frame.HasResponseStarted);
            Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)frame).OnStarting(_ => TaskUtilities.CompletedTask, null));
        }

        [Fact]
        public void InitializeHeadersResetsRequestHeaders()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
                SocketOutput = new MockSocketOuptut()
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

            var originalRequestHeaders = frame.RequestHeaders;
            frame.RequestHeaders = new FrameRequestHeaders();

            // Act
            frame.InitializeHeaders();

            // Assert
            Assert.Same(originalRequestHeaders, frame.RequestHeaders);
        }

        [Fact]
        public void InitializeHeadersResetsResponseHeaders()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
                SocketOutput = new MockSocketOuptut()
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

            var originalResponseHeaders = frame.ResponseHeaders;
            frame.ResponseHeaders = new FrameResponseHeaders();

            // Act
            frame.InitializeHeaders();

            // Assert
            Assert.Same(originalResponseHeaders, frame.ResponseHeaders);
        }

        [Fact]
        public void InitializeStreamsResetsStreams()
        {
            // Arrange
            var connectionContext = new ConnectionContext()
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = new KestrelServerOptions(),
                SocketOutput = new MockSocketOuptut()
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

            var messageBody = MessageBody.For(HttpVersion.Http11, (FrameRequestHeaders)frame.RequestHeaders, frame);
            frame.InitializeStreams(messageBody);

            var originalRequestBody = frame.RequestBody;
            var originalResponseBody = frame.ResponseBody;
            var originalDuplexStream = frame.DuplexStream;
            frame.RequestBody = new MemoryStream();
            frame.ResponseBody = new MemoryStream();
            frame.DuplexStream = new MemoryStream();

            // Act
            frame.InitializeStreams(messageBody);

            // Assert
            Assert.Same(originalRequestBody, frame.RequestBody);
            Assert.Same(originalResponseBody, frame.ResponseBody);
            Assert.Same(originalDuplexStream, frame.DuplexStream);
        }
    }
}
