// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCompression.Tests
{
    public class ResponseCompressionMiddlewareTest
    {
        private const string TextPlain = "text/plain";

        public static IEnumerable<object[]> SupportedEncodings =>
            TestData.Select(x => new object[] { x.EncodingName });

        public static IEnumerable<object[]> SupportedEncodingsWithBodyLength =>
            TestData.Select(x => new object[] { x.EncodingName, x.ExpectedBodyLength });

        private static IEnumerable<EncodingTestData> TestData
        {
            get
            {
                yield return new EncodingTestData("gzip", expectedBodyLength: 24);
#if NETCOREAPP2_2
                yield return new EncodingTestData("br", expectedBodyLength: 20);
#elif NET461
#else
#error Target frameworks need to be updated.
#endif
            }
        }

        [Fact]
        public void Options_HttpsDisabledByDefault()
        {
            var options = new ResponseCompressionOptions();

            Assert.False(options.EnableForHttps);
        }

        [Fact]
        public async Task Request_NoAcceptEncoding_Uncompressed()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: null, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
        }

        [Fact]
        public async Task Request_AcceptGzipDeflate_CompressedGzip()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "gzip", "deflate" }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 24, expectedEncoding: "gzip");
        }

        [Fact]
        public async Task Request_AcceptBrotli_CompressedBrotli()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "br" }, responseType: TextPlain);

#if NET461
            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: true);
#elif NETCOREAPP2_2
            CheckResponseCompressed(response, expectedBodyLength: 20, expectedEncoding: "br");
#else
#error Target frameworks need to be updated.
#endif
        }

        [Theory]
        [InlineData("gzip", "br")]
        [InlineData("br", "gzip")]
        public async Task Request_AcceptMixed_CompressedBrotli(string encoding1, string encoding2)
        {
            var response = await InvokeMiddleware(100, new[] { encoding1, encoding2 }, responseType: TextPlain);

#if NET461
            CheckResponseCompressed(response, expectedBodyLength: 24, expectedEncoding: "gzip");
#elif NETCOREAPP2_2
            CheckResponseCompressed(response, expectedBodyLength: 20, expectedEncoding: "br");
#else
#error Target frameworks need to be updated.
#endif
        }

#if NETCOREAPP2_2
        [Theory]
        [InlineData("gzip", "br")]
        [InlineData("br", "gzip")]
        public async Task Request_AcceptMixed_ConfiguredOrder_CompressedGzip(string encoding1, string encoding2)
        {
            void Configure(ResponseCompressionOptions options)
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();
            }

            var response = await InvokeMiddleware(100, new[] { encoding1, encoding2 }, responseType: TextPlain, configure: Configure);

            CheckResponseCompressed(response, expectedBodyLength: 24, expectedEncoding: "gzip");
        }
#elif NET461
#else
#error Target frameworks need to be updated.
#endif

        [Fact]
        public async Task Request_AcceptUnknown_NotCompressed()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "unknown" }, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: true);
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/PLAIN")]
        [InlineData("text/plain; charset=ISO-8859-4")]
        [InlineData("text/plain ; charset=ISO-8859-4")]
        public async Task ContentType_WithCharset_Compress(string contentType)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = contentType;
                        return context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseCompressed(response, expectedBodyLength: 24, expectedEncoding: "gzip");
        }

        [Fact]
        public async Task GZipCompressionProvider_OptionsSetInDI_Compress()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.NoCompression);
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        return context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseCompressed(response, expectedBodyLength: 123, expectedEncoding: "gzip");
        }

        [Theory]
        [InlineData("")]
        [InlineData("text/plain2")]
        public async Task MimeTypes_OtherContentTypes_NoMatch(string contentType)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = contentType;
                        return context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
        }

        [Theory]
        [InlineData("")]
        [InlineData("text/plain")]
        [InlineData("text/PLAIN")]
        [InlineData("text/plain; charset=ISO-8859-4")]
        [InlineData("text/plain ; charset=ISO-8859-4")]
        [InlineData("text/plain2")]
        public async Task NoBody_NotCompressed(string contentType)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = contentType;
                        return Task.FromResult(0);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseNotCompressed(response, expectedBodyLength: 0, sendVaryHeader: false);
        }

        [Fact]
        public async Task Request_AcceptStar_Compressed()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "*" }, responseType: TextPlain);

#if NET461
            CheckResponseCompressed(response, expectedBodyLength: 24, expectedEncoding: "gzip");
#elif NETCOREAPP2_2
            CheckResponseCompressed(response, expectedBodyLength: 20, expectedEncoding: "br");
#else
#error Target frameworks need to be updated.
#endif
        }

        [Fact]
        public async Task Request_AcceptIdentity_NotCompressed()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "identity" }, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: true);
        }

        [Theory]
        [InlineData(new[] { "identity;q=0.5", "gzip;q=1" }, 24)]
        [InlineData(new[] { "identity;q=0", "gzip;q=0.8" }, 24)]
        [InlineData(new[] { "identity;q=0.5", "gzip" }, 24)]
        public async Task Request_AcceptWithHigherCompressionQuality_Compressed(string[] acceptEncodings, int expectedBodyLength)
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: acceptEncodings, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: expectedBodyLength, expectedEncoding: "gzip");
        }

        [Theory]
        [InlineData(new[] { "gzip;q=0.5", "identity;q=0.8" }, 100)]
        public async Task Request_AcceptWithhigherIdentityQuality_NotCompressed(string[] acceptEncodings, int expectedBodyLength)
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: acceptEncodings, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: expectedBodyLength, sendVaryHeader: true);
        }

        [Fact]
        public async Task Response_UnknownMimeType_NotCompressed()
        {
            var response = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "gzip" }, responseType: "text/custom");

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
        }

        [Fact]
        public async Task Response_WithContentRange_NotCompressed()
        {
            var response = await InvokeMiddleware(50, requestAcceptEncodings: new[] { "gzip" }, responseType: TextPlain, addResponseAction: (r) =>
            {
                r.Headers[HeaderNames.ContentRange] = "1-2/*";
            });

            CheckResponseNotCompressed(response, expectedBodyLength: 50, sendVaryHeader: false);
        }

        [Fact]
        public async Task Response_WithContentEncodingAlreadySet_Stacked()
        {
            var otherContentEncoding = "something";

            var response = await InvokeMiddleware(50, requestAcceptEncodings: new[] { "gzip" }, responseType: TextPlain, addResponseAction: (r) =>
            {
                r.Headers[HeaderNames.ContentEncoding] = otherContentEncoding;
            });

            Assert.True(response.Content.Headers.ContentEncoding.Contains(otherContentEncoding));
            Assert.True(response.Content.Headers.ContentEncoding.Contains("gzip"));
            Assert.Equal(24, response.Content.Headers.ContentLength);
        }

        [Theory]
        [InlineData(false, 100)]
        [InlineData(true, 24)]
        public async Task Request_Https_CompressedIfEnabled(bool enableHttps, int expectedLength)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression(options =>
                    {
                        options.EnableForHttps = enableHttps;
                        options.MimeTypes = new[] { TextPlain };
                    });
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.ContentType = TextPlain;
                        return context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder)
            {
                BaseAddress = new Uri("https://localhost/")
            };

            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            Assert.Equal(expectedLength, response.Content.ReadAsByteArrayAsync().Result.Length);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodingsWithBodyLength))]
        public async Task FlushHeaders_SendsHeaders_Compresses(string encoding, int expectedBodyLength)
        {
            var responseReceived = new ManualResetEvent(false);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Response.Body.Flush();
                        Assert.True(responseReceived.WaitOne(TimeSpan.FromSeconds(3)));
                        return context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            responseReceived.Set();

            await response.Content.LoadIntoBufferAsync();

            CheckResponseCompressed(response, expectedBodyLength, encoding);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodingsWithBodyLength))]
        public async Task FlushAsyncHeaders_SendsHeaders_Compresses(string encoding, int expectedBodyLength)
        {
            var responseReceived = new ManualResetEvent(false);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(async context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        await context.Response.Body.FlushAsync();
                        Assert.True(responseReceived.WaitOne(TimeSpan.FromSeconds(3)));
                        await context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            responseReceived.Set();

            await response.Content.LoadIntoBufferAsync();

            CheckResponseCompressed(response, expectedBodyLength, encoding);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task FlushBody_CompressesAndFlushes(string encoding)
        {
            var responseReceived = new ManualResetEvent(false);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Response.Body.Write(new byte[10], 0, 10);
                        context.Response.Body.Flush();
                        Assert.True(responseReceived.WaitOne(TimeSpan.FromSeconds(3)));
                        context.Response.Body.Write(new byte[90], 0, 90);
                        return Task.FromResult(0);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.False(response.Content.Headers.TryGetValues(HeaderNames.ContentMD5, out _));
            Assert.Single(response.Content.Headers.ContentEncoding, encoding);

            var body = await response.Content.ReadAsStreamAsync();
            var read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);

            responseReceived.Set();

            read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task FlushAsyncBody_CompressesAndFlushes(string encoding)
        {
            var responseReceived = new ManualResetEvent(false);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(async context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        await context.Response.WriteAsync(new string('a', 10));
                        await context.Response.Body.FlushAsync();
                        Assert.True(responseReceived.WaitOne(TimeSpan.FromSeconds(3)));
                        await context.Response.WriteAsync(new string('a', 90));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.False(response.Content.Headers.TryGetValues(HeaderNames.ContentMD5, out _));
            Assert.Single(response.Content.Headers.ContentEncoding, encoding);

            var body = await response.Content.ReadAsStreamAsync();
            var read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);

            responseReceived.Set();

            read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task TrickleWriteAndFlush_FlushesEachWrite(string encoding)
        {
            var responseReceived = new[]
            {
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
            };

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Features.Get<IHttpBufferingFeature>()?.DisableResponseBuffering();

                        foreach (var signal in responseReceived)
                        {
                            context.Response.Body.Write(new byte[1], 0, 1);
                            context.Response.Body.Flush();
                            Assert.True(signal.WaitOne(TimeSpan.FromSeconds(3)));
                        }
                        return Task.FromResult(0);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

#if NET461 // Flush not supported, compression disabled
            Assert.NotNull(response.Content.Headers.GetValues(HeaderNames.ContentMD5));
            Assert.Empty(response.Content.Headers.ContentEncoding);
#elif NETCOREAPP2_2 // Flush supported, compression enabled
            Assert.False(response.Content.Headers.TryGetValues(HeaderNames.ContentMD5, out _));
            Assert.Single(response.Content.Headers.ContentEncoding, encoding);
#else
#error Target frameworks need to be updated.
#endif

            var body = await response.Content.ReadAsStreamAsync();

            foreach (var signal in responseReceived)
            {
                var read = await body.ReadAsync(new byte[100], 0, 100);
                Assert.True(read > 0);

                signal.Set();
            }
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task TrickleWriteAndFlushAsync_FlushesEachWrite(string encoding)
        {
            var responseReceived = new[]
            {
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
            };

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(async context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Features.Get<IHttpBufferingFeature>()?.DisableResponseBuffering();

                        foreach (var signal in responseReceived)
                        {
                            await context.Response.WriteAsync("a");
                            await context.Response.Body.FlushAsync();
                            Assert.True(signal.WaitOne(TimeSpan.FromSeconds(3)));
                        }
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

#if NET461 // Flush not supported, compression disabled
            Assert.NotNull(response.Content.Headers.GetValues(HeaderNames.ContentMD5));
            Assert.Empty(response.Content.Headers.ContentEncoding);
#elif NETCOREAPP2_2 // Flush supported, compression enabled
            Assert.False(response.Content.Headers.TryGetValues(HeaderNames.ContentMD5, out _));
            Assert.Single(response.Content.Headers.ContentEncoding, encoding);
#else
#error Target framework needs to be updated
#endif

            var body = await response.Content.ReadAsStreamAsync();

            foreach (var signal in responseReceived)
            {
                var read = await body.ReadAsync(new byte[100], 0, 100);
                Assert.True(read > 0);

                signal.Set();
            }
        }

        [Fact]
        public async Task SendFileAsync_OnlySetIfFeatureAlreadyExists()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Response.ContentLength = 1024;
                        var sendFile = context.Features.Get<IHttpSendFileFeature>();
                        Assert.Null(sendFile);
                        return Task.FromResult(0);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task SendFileAsync_DifferentContentType_NotBypassed()
        {
            FakeSendFileFeature fakeSendFile = null;

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        fakeSendFile = new FakeSendFileFeature(context.Response.Body);
                        context.Features.Set<IHttpSendFileFeature>(fakeSendFile);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = "custom/type";
                        context.Response.ContentLength = 1024;
                        var sendFile = context.Features.Get<IHttpSendFileFeature>();
                        Assert.NotNull(sendFile);
                        return sendFile.SendFileAsync("testfile1kb.txt", 0, null, CancellationToken.None);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseNotCompressed(response, expectedBodyLength: 1024, sendVaryHeader: false);

            Assert.True(fakeSendFile.Invoked);
        }

        [Fact]
        public async Task SendFileAsync_FirstWrite_CompressesAndFlushes()
        {
            FakeSendFileFeature fakeSendFile = null;

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        fakeSendFile = new FakeSendFileFeature(context.Response.Body);
                        context.Features.Set<IHttpSendFileFeature>(fakeSendFile);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Response.ContentLength = 1024;
                        var sendFile = context.Features.Get<IHttpSendFileFeature>();
                        Assert.NotNull(sendFile);
                        return sendFile.SendFileAsync("testfile1kb.txt", 0, null, CancellationToken.None);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseCompressed(response, expectedBodyLength: 34, expectedEncoding: "gzip");

            Assert.False(fakeSendFile.Invoked);
        }

        [Fact]
        public async Task SendFileAsync_AfterFirstWrite_CompressesAndFlushes()
        {
            FakeSendFileFeature fakeSendFile = null;

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        fakeSendFile = new FakeSendFileFeature(context.Response.Body);
                        context.Features.Set<IHttpSendFileFeature>(fakeSendFile);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(async context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        var sendFile = context.Features.Get<IHttpSendFileFeature>();
                        Assert.NotNull(sendFile);

                        await context.Response.WriteAsync(new string('a', 100));
                        await sendFile.SendFileAsync("testfile1kb.txt", 0, null, CancellationToken.None);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseCompressed(response, expectedBodyLength: 40, expectedEncoding: "gzip");

            Assert.False(fakeSendFile.Invoked);
        }

        private Task<HttpResponseMessage> InvokeMiddleware(
            int uncompressedBodyLength,
            string[] requestAcceptEncodings,
            string responseType,
            Action<HttpResponse> addResponseAction = null,
            Action<ResponseCompressionOptions> configure = null)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression(configure ?? (_ => { }));
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = responseType;
                        Assert.Null(context.Features.Get<IHttpSendFileFeature>());
                        addResponseAction?.Invoke(context.Response);
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

        private void CheckResponseCompressed(HttpResponseMessage response, int expectedBodyLength, string expectedEncoding)
        {
            var containsVaryAcceptEncoding = false;
            foreach (var value in response.Headers.GetValues(HeaderNames.Vary))
            {
                if (value.Contains(HeaderNames.AcceptEncoding))
                {
                    containsVaryAcceptEncoding = true;
                    break;
                }
            }
            Assert.True(containsVaryAcceptEncoding);
            Assert.False(response.Content.Headers.TryGetValues(HeaderNames.ContentMD5, out _));
            Assert.Single(response.Content.Headers.ContentEncoding, expectedEncoding);
            Assert.Equal(expectedBodyLength, response.Content.Headers.ContentLength);
        }

        private void CheckResponseNotCompressed(HttpResponseMessage response, int expectedBodyLength, bool sendVaryHeader)
        {
            if (sendVaryHeader)
            {
                var containsVaryAcceptEncoding = false;
                foreach (var value in response.Headers.GetValues(HeaderNames.Vary))
                {
                    if (value.Contains(HeaderNames.AcceptEncoding))
                    {
                        containsVaryAcceptEncoding = true;
                        break;
                    }
                }
                Assert.True(containsVaryAcceptEncoding);
            }
            else
            {
                Assert.False(response.Headers.Contains(HeaderNames.Vary));
            }
            Assert.NotNull(response.Content.Headers.GetValues(HeaderNames.ContentMD5));
            Assert.Empty(response.Content.Headers.ContentEncoding);
            Assert.Equal(expectedBodyLength, response.Content.Headers.ContentLength);
        }

        private class FakeSendFileFeature : IHttpSendFileFeature
        {
            private readonly Stream _innerBody;

            public FakeSendFileFeature(Stream innerBody)
            {
                _innerBody = innerBody;
            }

            public bool Invoked { get; set; }

            public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
            {
                // This implementation should only be delegated to if compression is disabled.
                Invoked = true;
                using (var file = new FileStream(path, FileMode.Open))
                {
                    file.Seek(offset, SeekOrigin.Begin);
                    if (count.HasValue)
                    {
                        throw new NotImplementedException("Not implemented for testing");
                    }
                    await file.CopyToAsync(_innerBody, 81920, cancellation);
                }
            }
        }

        private readonly struct EncodingTestData
        {
            public EncodingTestData(string encodingName, int expectedBodyLength)
            {
                EncodingName = encodingName;
                ExpectedBodyLength = expectedBodyLength;
            }

            public string EncodingName { get; }

            public int ExpectedBodyLength { get; }
        }
    }
}
