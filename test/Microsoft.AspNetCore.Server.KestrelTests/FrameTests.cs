// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameTests
    {
        [Theory]
        [InlineData("Cookie: \r\n\r\n", 1)]
        [InlineData("Cookie:\r\n\r\n", 1)]
        [InlineData("Cookie:\r\n value\r\n\r\n", 1)]
        [InlineData("Cookie\r\n", 0)]
        [InlineData("Cookie: \r\nConnection: close\r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie: \r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie \r\n", 1)]
        [InlineData("Connection:\r\n \r\nCookie \r\n", 1)]
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
                    ServerAddress = ServerAddress.FromUrl("http://localhost:5000")
                };
                var frame = new Frame<object>(application: null, context: connectionContext);
                var headerCollection = new FrameRequestHeaders();

                var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
                socketInput.IncomingData(headerArray, 0, headerArray.Length);

                var success = frame.TakeMessageHeaders(socketInput, headerCollection);

                Assert.True(success);
                Assert.Equal(numHeaders, headerCollection.Count());

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
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000")
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.Scheme = "https";

            // Act
            frame.Reset();

            // Assert
            Assert.Equal("http", ((IFeatureCollection)frame).Get<IHttpRequestFeature>().Scheme);
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

            // Act
            var originalRequestHeaders = frame.RequestHeaders;
            frame.RequestHeaders = new FrameRequestHeaders();
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

            // Act
            var originalResponseHeaders = frame.ResponseHeaders;
            frame.ResponseHeaders = new FrameResponseHeaders();
            frame.InitializeHeaders();

            // Assert
            Assert.Same(originalResponseHeaders, frame.ResponseHeaders);
        }
    }
}
