// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpRequestMessageFeatureTest
    {
        [Fact]
        public void HttpRequestMessage_CombinesUri()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var feature = new HttpRequestMessageFeature(context);

            context.Request.Method = "GET";

            context.Request.Scheme = "http";
            context.Request.Host = new HostString("contoso.com");
            context.Request.PathBase = new PathString("/app");
            context.Request.Path = new PathString("/api/Products");
            context.Request.QueryString = new QueryString("?orderId=3");

            // Act
            var request = feature.HttpRequestMessage;

            // Assert
            Assert.Equal("http://contoso.com/app/api/Products?orderId=3", request.RequestUri.AbsoluteUri);
        }

        [Fact]
        public void HttpRequestMessage_CopiesRequestMethod()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var feature = new HttpRequestMessageFeature(context);

            context.Request.Method = "OPTIONS";

            // Act
            var request = feature.HttpRequestMessage;

            // Assert
            Assert.Equal(new HttpMethod("OPTIONS"), request.Method);
        }

        [Fact]
        public void HttpRequestMessage_CopiesHeader()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var feature = new HttpRequestMessageFeature(context);

            context.Request.Method = "OPTIONS";

            context.Request.Headers.Add("Host", new string[] { "contoso.com" });

            // Act
            var request = feature.HttpRequestMessage;

            // Assert
            Assert.Equal("contoso.com", request.Headers.Host);
        }

        [Fact]
        public void HttpRequestMessage_CopiesContentHeader()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var feature = new HttpRequestMessageFeature(context);

            context.Request.Method = "OPTIONS";

            context.Request.Headers.Add("Content-Type", new string[] { "text/plain" });

            // Act
            var request = feature.HttpRequestMessage;

            // Assert
            Assert.Equal("text/plain", request.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task HttpRequestMessage_WrapsBodyContent()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var feature = new HttpRequestMessageFeature(context);

            context.Request.Method = "OPTIONS";

            var bytes = Encoding.UTF8.GetBytes("Hello, world!");
            context.Request.Body = new MemoryStream(bytes);
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            // Act
            var request = feature.HttpRequestMessage;

            // Assert
            var streamContent = Assert.IsType<StreamContent>(request.Content);
            var content = await request.Content.ReadAsStringAsync();
            Assert.Equal("Hello, world!", content);
        }

        [Fact]
        public void HttpRequestMessage_CachesMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var feature = new HttpRequestMessageFeature(context);

            context.Request.Method = "GET";
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("contoso.com");

            // Act
            var request1 = feature.HttpRequestMessage;

            context.Request.Path = new PathString("/api/Products");
            var request2 = feature.HttpRequestMessage;

            // Assert
            Assert.Same(request1, request2);
            Assert.Equal("/", request2.RequestUri.AbsolutePath);
        }
    }
}