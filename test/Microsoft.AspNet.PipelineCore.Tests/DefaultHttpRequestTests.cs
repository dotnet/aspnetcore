// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Xunit;

namespace Microsoft.AspNet.PipelineCore.Tests
{
    public class DefaultHttpRequestTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(9001)]
        [InlineData(65535)]
        public void GetContentLength_ReturnsParsedHeader(long value)
        {
            // Arrange
            var request = GetRequestWithContentLength(value.ToString(CultureInfo.InvariantCulture));

            // Act and Assert
            Assert.Equal(value, request.ContentLength);
        }

        [Fact]
        public void GetContentLength_ReturnsNullIfHeaderDoesNotExist()
        {
            // Arrange
            var request = GetRequestWithContentLength(contentLength: null);

            // Act and Assert
            Assert.Null(request.ContentLength);
        }

        [Theory]
        [InlineData("cant-parse-this")]
        [InlineData("-1000")]
        [InlineData("1000.00")]
        [InlineData("100/5")]
        public void GetContentLength_ReturnsNullIfHeaderCannotBeParsed(string contentLength)
        {
            // Arrange
            var request = GetRequestWithContentLength(contentLength);

            // Act and Assert
            Assert.Null(request.ContentLength);
        }

        [Fact]
        public void Host_GetsHostFromHeaders()
        {
            // Arrange
            const string expected = "localhost:9001";

            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Host", new string[] { expected } },
            };

            var request = CreateRequest(headers);

            // Act
            var host = request.Host;

            // Assert
            Assert.Equal(expected, host.Value);
        }

        [Fact]
        public void Host_DecodesPunyCode()
        {
            // Arrange
            const string expected = "löcalhöst";

            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Host", new string[]{ "xn--lcalhst-90ae" } },
            };

            var request = CreateRequest(headers);

            // Act
            var host = request.Host;

            // Assert
            Assert.Equal(expected, host.Value);
        }

        [Fact]
        public void Host_EncodesPunyCode()
        {
            // Arrange
            const string expected = "xn--lcalhst-90ae";

            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            var request = CreateRequest(headers);

            // Act
            request.Host = new HostString("löcalhöst");

            // Assert
            Assert.Equal(expected, headers["Host"][0]);
        }

        private static HttpRequest CreateRequest(IDictionary<string, string[]> headers)
        {
            var context = new DefaultHttpContext();
            context.GetFeature<IHttpRequestFeature>().Headers = headers;
            return context.Request;
        }

        private static HttpRequest GetRequestWithContentLength(string contentLength = null)
        {
            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (contentLength != null)
            {
                headers.Add("Content-Length", new[] { contentLength });
            }

 		    return CreateRequest(headers);
        }
    }
}
