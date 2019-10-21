// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class DefaultHttpResponseTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(9001)]
        [InlineData(65535)]
        public void GetContentLength_ReturnsParsedHeader(long value)
        {
            // Arrange
            var response = GetResponseWithContentLength(value.ToString(CultureInfo.InvariantCulture));

            // Act and Assert
            Assert.Equal(value, response.ContentLength);
        }

        [Fact]
        public void GetContentLength_ReturnsNullIfHeaderDoesNotExist()
        {
            // Arrange
            var response = GetResponseWithContentLength(contentLength: null);

            // Act and Assert
            Assert.Null(response.ContentLength);
        }

        [Theory]
        [InlineData("cant-parse-this")]
        [InlineData("-1000")]
        [InlineData("1000.00")]
        [InlineData("100/5")]
        public void GetContentLength_ReturnsNullIfHeaderCannotBeParsed(string contentLength)
        {
            // Arrange
            var response = GetResponseWithContentLength(contentLength);

            // Act and Assert
            Assert.Null(response.ContentLength);
        }

        [Fact]
        public void GetContentType_ReturnsNullIfHeaderDoesNotExist()
        {
            // Arrange
            var response = GetResponseWithContentType(contentType: null);

            // Act and Assert
            Assert.Null(response.ContentType);
        }

        [Fact]
        public void BodyWriter_CanGet()
        {
            var response = new DefaultHttpContext();
            var bodyPipe = response.Response.BodyWriter;

            Assert.NotNull(bodyPipe);
        }

        [Fact]
        public async Task ResponseStart_CallsFeatureIfSet()
        {
            var features = new FeatureCollection();
            var mock = new Mock<IHttpResponseBodyFeature>();
            mock.Setup(o => o.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            features.Set(mock.Object);

            var responseMock = new Mock<IHttpResponseFeature>();
            responseMock.Setup(o => o.HasStarted).Returns(false);
            features.Set(responseMock.Object);

            var context = new DefaultHttpContext(features);
            await context.Response.StartAsync();

            mock.Verify(m => m.StartAsync(default), Times.Once());
        }

        [Fact]
        public async Task ResponseStart_CallsFeatureIfSetWithProvidedCancellationToken()
        {
            var features = new FeatureCollection();

            var mock = new Mock<IHttpResponseBodyFeature>();
            var ct = new CancellationToken();
            mock.Setup(o => o.StartAsync(It.Is<CancellationToken>((localCt) => localCt.Equals(ct)))).Returns(Task.CompletedTask);
            features.Set(mock.Object);

            var responseMock = new Mock<IHttpResponseFeature>();
            responseMock.Setup(o => o.HasStarted).Returns(false);
            features.Set(responseMock.Object);

            var context = new DefaultHttpContext(features);
            await context.Response.StartAsync(ct);

            mock.Verify(m => m.StartAsync(default), Times.Once());
        }

        [Fact]
        public async Task ResponseStart_DoesNotCallStartIfHasStartedIsTrue()
        {
            var features = new FeatureCollection();

            var startMock = new Mock<IHttpResponseBodyFeature>();
            startMock.Setup(o => o.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            features.Set(startMock.Object);

            var responseMock = new Mock<IHttpResponseFeature>();
            responseMock.Setup(o => o.HasStarted).Returns(true);
            features.Set(responseMock.Object);

            var context = new DefaultHttpContext(features);
            await context.Response.StartAsync();

            startMock.Verify(m => m.StartAsync(default), Times.Never());
        }

        [Fact]
        public async Task ResponseStart_CallsResponseBodyFlushIfNotSet()
        {
            var context = new DefaultHttpContext();
            var mock = new FlushAsyncCheckStream();
            context.Response.Body = mock;

            await context.Response.StartAsync(default);

            Assert.True(mock.IsCalled);
        }

        private static HttpResponse CreateResponse(IHeaderDictionary headers)
        {
            var context = new DefaultHttpContext();
            context.Features.Get<IHttpResponseFeature>().Headers = headers;
            return context.Response;
        }

        private static HttpResponse GetResponseWithContentLength(string contentLength = null)
        {
            return GetResponseWithHeader("Content-Length", contentLength);
        }

        private static HttpResponse GetResponseWithContentType(string contentType = null)
        {
            return GetResponseWithHeader("Content-Type", contentType);
        }

        private static HttpResponse GetResponseWithHeader(string headerName, string headerValue)
        {
            var headers = new HeaderDictionary();
            if (headerValue != null)
            {
                headers.Add(headerName, headerValue);
            }

            return CreateResponse(headers);
        }

        private class FlushAsyncCheckStream : MemoryStream
        {
            public bool IsCalled { get; private set; }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                IsCalled = true;
                return base.FlushAsync(cancellationToken);
            }
        }
    }
}
