// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCompression.Tests
{
    public class ResponseCompressionMiddlewareTest
    {
        private const string TextPlain = "text/plain";

        [Fact]
        public void Options_NullShouldCompressResponse()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ResponseCompressionMiddleware(null, Options.Create(new ResponseCompressionOptions()
                {
                    ShouldCompressResponse = null
                }));
            });
        }

        [Fact]
        public void Options_HttpsDisabledByDefault()
        {
            var options = new ResponseCompressionOptions();

            Assert.False(options.EnableHttps);
        }

        [Fact]
        public void Options_EmptyProviderList()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ResponseCompressionMiddleware(null, Options.Create(new ResponseCompressionOptions()
                {
                    ShouldCompressResponse = _ => true,
                    Providers = new IResponseCompressionProvider[0]
                }));
            });
        }

        [Fact]
        public async Task Request_Uncompressed()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: null, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100);
        }

        [Fact]
        public async Task Request_CompressGzip()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new string[] { "gzip", "deflate" }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 24);
        }

        [Fact]
        public async Task Request_CompressUnknown()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new string[] { "unknown" }, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100);
        }

        [Fact]
        public async Task Request_CompressStar()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new string[] { "*" }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 24);
        }

        [Fact]
        public async Task Request_CompressIdentity()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new string[] { "identity" }, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100);
        }

        [Theory]
        [InlineData(new string[] { "identity;q=0.5", "gzip;q=1" }, 24)]
        [InlineData(new string[] { "identity;q=0", "gzip;q=0.8" }, 24)]
        [InlineData(new string[] { "identity;q=0.5", "gzip" }, 24)]
        public async Task Request_CompressQuality_Compressed(string[] acceptEncodings, int expectedBodyLength)
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: acceptEncodings, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: expectedBodyLength);
        }

        [Theory]
        [InlineData(new string[] { "gzip;q=0.5", "identity;q=0.8" }, 100)]
        public async Task Request_CompressQuality_NotCompressed(string[] acceptEncodings, int expectedBodyLength)
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: acceptEncodings, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: expectedBodyLength);
        }

        [Fact]
        public async Task Request_UnauthorizedMimeType()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new string[] { "gzip" }, responseType: "text/html");

            CheckResponseNotCompressed(response, expectedBodyLength: 100);
        }

        [Fact]
        public async Task Request_ResponseWithContentRange()
        {
            var response = await InvokeMiddleware(50, requestAcceptEncodings: new string[] { "gzip" }, responseType: TextPlain, addResponseAction: (r) =>
            {
                r.Headers[HeaderNames.ContentRange] = "1-2/*";
            });

            CheckResponseNotCompressed(response, expectedBodyLength: 50);
        }

        [Fact]
        public async Task Request_ResponseWithContentEncodingAlreadySet()
        {
            var otherContentEncoding = "something";

            var response = await InvokeMiddleware(50, requestAcceptEncodings: new string[] { "gzip" }, responseType: TextPlain, addResponseAction: (r) =>
            {
                r.Headers[HeaderNames.ContentEncoding] = otherContentEncoding;
            });

            Assert.NotNull(response.Headers.GetValues(HeaderNames.ContentMD5));
            Assert.Single(response.Content.Headers.ContentEncoding, otherContentEncoding);
            Assert.Equal(50, response.Content.Headers.ContentLength);
        }

        [Theory]
        [InlineData(false, 100)]
        [InlineData(true, 24)]
        public async Task Request_Https(bool enableHttps, int expectedLength)
        {
            var options = new ResponseCompressionOptions()
            {
                ShouldCompressResponse = _ => true,
                Providers = new IResponseCompressionProvider[]
                {
                    new GzipResponseCompressionProvider(CompressionLevel.Optimal)
                },
                EnableHttps = enableHttps
            };

            var middleware = new ResponseCompressionMiddleware(async context =>
            {
                context.Response.ContentType = TextPlain;
                await context.Response.WriteAsync(new string('a', 100));
            }, Options.Create(options));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[HeaderNames.AcceptEncoding] = "gzip";
            httpContext.Request.IsHttps = true;

            httpContext.Response.Body = new MemoryStream();

            await middleware.Invoke(httpContext);

            Assert.Equal(expectedLength, httpContext.Response.Body.Length);
        }

        private Task<HttpResponseMessage> InvokeMiddleware(int uncompressedBodyLength, string[] requestAcceptEncodings, string responseType, Action<HttpResponse> addResponseAction = null)
        {
            var options = new ResponseCompressionOptions()
            {
                ShouldCompressResponse = ctx =>
                {
                    var contentType = ctx.Response.Headers[HeaderNames.ContentType];
                    return contentType.ToString().IndexOf(TextPlain) >= 0;
                },
                Providers = new IResponseCompressionProvider[]
                {
                    new GzipResponseCompressionProvider(CompressionLevel.Optimal)
                }
            };

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseResponseCompression(options);
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = responseType;
                        if (addResponseAction != null)
                        {
                            addResponseAction(context.Response);
                        }
                        return context.Response.WriteAsync(new string('a', uncompressedBodyLength));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            for (var i = 0; i < requestAcceptEncodings?.Length; i++)
            {
                request.Headers.AcceptEncoding.Add(System.Net.Http.Headers.StringWithQualityHeaderValue.Parse(requestAcceptEncodings[i]));
            }

            return client.SendAsync(request);
        }

        private void CheckResponseCompressed(HttpResponseMessage response, int expectedBodyLength)
        {
            IEnumerable<string> contentMD5 = null;

            Assert.False(response.Headers.TryGetValues(HeaderNames.ContentMD5, out contentMD5));
            Assert.Single(response.Content.Headers.ContentEncoding, "gzip");
            Assert.Equal(expectedBodyLength, response.Content.Headers.ContentLength);
        }

        private void CheckResponseNotCompressed(HttpResponseMessage response, int expectedBodyLength)
        {
            Assert.NotNull(response.Headers.GetValues(HeaderNames.ContentMD5));
            Assert.Empty(response.Content.Headers.ContentEncoding);
            Assert.Equal(expectedBodyLength, response.Content.Headers.ContentLength);
        }
    }
}
