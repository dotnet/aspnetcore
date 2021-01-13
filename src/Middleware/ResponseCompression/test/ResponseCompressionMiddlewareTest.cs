// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
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
                yield return new EncodingTestData("gzip", expectedBodyLength: 30);
                yield return new EncodingTestData("br", expectedBodyLength: 21);
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
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: null, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
            AssertLog(logMessages.Single(), LogLevel.Debug, "No response compression available, the Accept-Encoding header is missing or invalid.");
        }

        [Fact]
        public async Task Request_AcceptGzipDeflate_CompressedGzip()
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "gzip", "deflate" }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 30, expectedEncoding: "gzip");
            AssertCompressedWithLog(logMessages, "gzip");
        }

        [Fact]
        public async Task Request_AcceptBrotli_CompressedBrotli()
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "br" }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 21, expectedEncoding: "br");
            AssertCompressedWithLog(logMessages, "br");
        }

        [Theory]
        [InlineData("gzip", "br")]
        [InlineData("br", "gzip")]
        public async Task Request_AcceptMixed_CompressedBrotli(string encoding1, string encoding2)
        {
            var (response, logMessages) = await InvokeMiddleware(100, new[] { encoding1, encoding2 }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 21, expectedEncoding: "br");
            AssertCompressedWithLog(logMessages, "br");
        }

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

            var (response, logMessages) = await InvokeMiddleware(100, new[] { encoding1, encoding2 }, responseType: TextPlain, configure: Configure);

            CheckResponseCompressed(response, expectedBodyLength: 30, expectedEncoding: "gzip");
            AssertCompressedWithLog(logMessages, "gzip");
        }

        [Fact]
        public async Task Request_AcceptUnknown_NotCompressed()
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "unknown" }, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: true);
            Assert.Equal(3, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, "Response compression is available for this Content-Type.");
            AssertLog(logMessages.Skip(2).First(), LogLevel.Debug, "No matching response compression provider found.");
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/PLAIN")]
        [InlineData("text/plain; charset=ISO-8859-4")]
        [InlineData("text/plain ; charset=ISO-8859-4")]
        public async Task ContentType_WithCharset_Compress(string contentType)
        {
            var (response, logMessages) = await InvokeMiddleware(uncompressedBodyLength: 100, requestAcceptEncodings: new[] { "gzip" }, contentType);

            CheckResponseCompressed(response, expectedBodyLength: 30, expectedEncoding: "gzip");
            AssertCompressedWithLog(logMessages, "gzip");
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

            CheckResponseCompressed(response, expectedBodyLength: 133, expectedEncoding: "gzip");
        }

        [Theory]
        [InlineData("")]
        [InlineData("text/plain2")]
        public async Task MimeTypes_OtherContentTypes_NoMatch(string contentType)
        {
            var (response, logMessages) = await InvokeMiddleware(uncompressedBodyLength: 100, requestAcceptEncodings: new[] { "gzip" }, contentType);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
            Assert.Equal(2, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            var expected = string.IsNullOrEmpty(contentType) ? "(null)" : contentType;
            AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, $"Response compression is not enabled for the Content-Type '{expected}'.");
        }

        [Theory]
        [InlineData(null, null, "text/plain", true)]
        [InlineData(null, new string[0], "text/plain", true)]
        [InlineData(null, new[] { "TEXT/plain" }, "text/plain", false)]
        [InlineData(null, new[] { "TEXT/*" }, "text/plain", true)]
        [InlineData(null, new[] { "*/*" }, "text/plain", true)]

        [InlineData(new string[0], null, "text/plain", true)]
        [InlineData(new string[0], new string[0], "text/plain", true)]
        [InlineData(new string[0], new[] { "TEXT/plain" }, "text/plain", false)]
        [InlineData(new string[0], new[] { "TEXT/*" }, "text/plain", true)]
        [InlineData(new string[0], new[] { "*/*" }, "text/plain", true)]

        [InlineData(new[] { "TEXT/plain" }, null, "text/plain", true)]
        [InlineData(new[] { "TEXT/plain" }, new string[0], "text/plain", true)]
        [InlineData(new[] { "TEXT/plain" }, new[] { "TEXT/plain" }, "text/plain", false)]
        [InlineData(new[] { "TEXT/plain" }, new[] { "TEXT/*" }, "text/plain", true)]
        [InlineData(new[] { "TEXT/plain" }, new[] { "*/*" }, "text/plain", true)]

        [InlineData(new[] { "TEXT/*" }, null, "text/plain", true)]
        [InlineData(new[] { "TEXT/*" }, new string[0], "text/plain", true)]
        [InlineData(new[] { "TEXT/*" }, new[] { "TEXT/plain" }, "text/plain", false)]
        [InlineData(new[] { "TEXT/*" }, new[] { "TEXT/*" }, "text/plain", false)]
        [InlineData(new[] { "TEXT/*" }, new[] { "*/*" }, "text/plain", true)]

        [InlineData(new[] { "*/*" }, null, "text/plain", true)]
        [InlineData(new[] { "*/*" }, new string[0], "text/plain", true)]
        [InlineData(new[] { "*/*" }, new[] { "TEXT/plain" }, "text/plain", false)]
        [InlineData(new[] { "*/*" }, new[] { "TEXT/*" }, "text/plain", false)]
        [InlineData(new[] { "*/*" }, new[] { "*/*" }, "text/plain", true)]

        [InlineData(null, null, "text/plain2", false)]
        [InlineData(null, new string[0], "text/plain2", false)]
        [InlineData(null, new[] { "TEXT/plain" }, "text/plain2", false)]
        [InlineData(null, new[] { "TEXT/*" }, "text/plain2", false)]
        [InlineData(null, new[] { "*/*" }, "text/plain2", false)]

        [InlineData(new string[0], null, "text/plain2", false)]
        [InlineData(new string[0], new string[0], "text/plain2", false)]
        [InlineData(new string[0], new[] { "TEXT/plain" }, "text/plain2", false)]
        [InlineData(new string[0], new[] { "TEXT/*" }, "text/plain2", false)]
        [InlineData(new string[0], new[] { "*/*" }, "text/plain2", false)]

        [InlineData(new[] { "TEXT/plain" }, null, "text/plain2", false)]
        [InlineData(new[] { "TEXT/plain" }, new string[0], "text/plain2", false)]
        [InlineData(new[] { "TEXT/plain" }, new[] { "TEXT/plain" }, "text/plain2", false)]
        [InlineData(new[] { "TEXT/plain" }, new[] { "TEXT/*" }, "text/plain2", false)]
        [InlineData(new[] { "TEXT/plain" }, new[] { "*/*" }, "text/plain2", false)]

        [InlineData(new[] { "TEXT/*" }, null, "text/plain2", true)]
        [InlineData(new[] { "TEXT/*" }, new string[0], "text/plain2", true)]
        [InlineData(new[] { "TEXT/*" }, new[] { "TEXT/plain" }, "text/plain2", true)]
        [InlineData(new[] { "TEXT/*" }, new[] { "TEXT/*" }, "text/plain2", false)]
        [InlineData(new[] { "TEXT/*" }, new[] { "*/*" }, "text/plain2", true)]

        [InlineData(new[] { "*/*" }, null, "text/plain2", true)]
        [InlineData(new[] { "*/*" }, new string[0], "text/plain2", true)]
        [InlineData(new[] { "*/*" }, new[] { "TEXT/plain" }, "text/plain2", true)]
        [InlineData(new[] { "*/*" }, new[] { "TEXT/*" }, "text/plain2", false)]
        [InlineData(new[] { "*/*" }, new[] { "*/*" }, "text/plain2", true)]
        public async Task MimeTypes_IncludedAndExcluded(
            string[] mimeTypes,
            string[] excludedMimeTypes,
            string contentType,
            bool compress
            )
        {
            var (response, logMessages) = await InvokeMiddleware(uncompressedBodyLength: 100, requestAcceptEncodings: new[] { "gzip" }, contentType,
                configure: options =>
                {
                    options.MimeTypes = mimeTypes;
                    options.ExcludedMimeTypes = excludedMimeTypes;
                });

            if (compress)
            {
                CheckResponseCompressed(response, expectedBodyLength: 30, expectedEncoding: "gzip");
                AssertCompressedWithLog(logMessages, "gzip");
            }
            else
            {
                CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
                Assert.Equal(2, logMessages.Count);
                AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
                AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, $"Response compression is not enabled for the Content-Type '{contentType}'.");
            }
        }

        [Fact]
        public async Task NoIncludedMimeTypes_UseDefaults()
        {
            var (response, logMessages) = await InvokeMiddleware(uncompressedBodyLength: 100, requestAcceptEncodings: new[] { "gzip" }, TextPlain,
                configure: options =>
                {
                    options.ExcludedMimeTypes = new[] { "text/*" };
                });

            CheckResponseCompressed(response, expectedBodyLength: 30, expectedEncoding: "gzip");
            AssertCompressedWithLog(logMessages, "gzip");
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
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "*" }, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: 21, expectedEncoding: "br");
            AssertCompressedWithLog(logMessages, "br");
        }

        [Fact]
        public async Task Request_AcceptIdentity_NotCompressed()
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "identity" }, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: true);
            Assert.Equal(3, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, "Response compression is available for this Content-Type.");
            AssertLog(logMessages.Skip(2).First(), LogLevel.Debug, "No matching response compression provider found.");
        }

        [Theory]
        [InlineData(new[] { "identity;q=0.5", "gzip;q=1" }, 30)]
        [InlineData(new[] { "identity;q=0", "gzip;q=0.8" }, 30)]
        [InlineData(new[] { "identity;q=0.5", "gzip" }, 30)]
        public async Task Request_AcceptWithHigherCompressionQuality_Compressed(string[] acceptEncodings, int expectedBodyLength)
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: acceptEncodings, responseType: TextPlain);

            CheckResponseCompressed(response, expectedBodyLength: expectedBodyLength, expectedEncoding: "gzip");
            AssertCompressedWithLog(logMessages, "gzip");
        }

        [Theory]
        [InlineData(new[] { "gzip;q=0.5", "identity;q=0.8" }, 100)]
        public async Task Request_AcceptWithhigherIdentityQuality_NotCompressed(string[] acceptEncodings, int expectedBodyLength)
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: acceptEncodings, responseType: TextPlain);

            CheckResponseNotCompressed(response, expectedBodyLength: expectedBodyLength, sendVaryHeader: true);
            Assert.Equal(3, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, "Response compression is available for this Content-Type.");
            AssertLog(logMessages.Skip(2).First(), LogLevel.Debug, "No matching response compression provider found.");
        }

        [Fact]
        public async Task Response_UnknownMimeType_NotCompressed()
        {
            var (response, logMessages) = await InvokeMiddleware(100, requestAcceptEncodings: new[] { "gzip" }, responseType: "text/custom");

            CheckResponseNotCompressed(response, expectedBodyLength: 100, sendVaryHeader: false);
            Assert.Equal(2, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, "Response compression is not enabled for the Content-Type 'text/custom'.");
        }

        [Fact]
        public async Task Response_WithContentRange_NotCompressed()
        {
            var (response, logMessages) = await InvokeMiddleware(50, requestAcceptEncodings: new[] { "gzip" }, responseType: TextPlain, addResponseAction: (r) =>
            {
                r.Headers[HeaderNames.ContentRange] = "1-2/*";
            });

            CheckResponseNotCompressed(response, expectedBodyLength: 50, sendVaryHeader: false);
            Assert.Equal(2, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, "Response compression disabled due to the Content-Range header.");
        }


        [Fact]
        public async Task Response_WithContentEncodingAlreadySet_NotReCompressed()
        {
            var otherContentEncoding = "something";

            var (response, logMessages) = await InvokeMiddleware(50, requestAcceptEncodings: new[] { "gzip" }, responseType: TextPlain, addResponseAction: (r) =>
            {
                r.Headers[HeaderNames.ContentEncoding] = otherContentEncoding;
            });

            Assert.True(response.Content.Headers.ContentEncoding.Contains(otherContentEncoding));
            Assert.False(response.Content.Headers.ContentEncoding.Contains("gzip"));
            Assert.Equal(50, response.Content.Headers.ContentLength);
            Assert.Equal(2, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, "Response compression disabled due to the Content-Encoding header.");
        }

        [Theory]
        [InlineData(false, 100)]
        [InlineData(true, 30)]
        public async Task Request_Https_CompressedIfEnabled(bool enableHttps, int expectedLength)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<ResponseCompressionProvider>,
                TestSink.EnableWithTypeName<ResponseCompressionProvider>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
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

            var logMessages = sink.Writes.ToList();
            if (enableHttps)
            {
                AssertCompressedWithLog(logMessages, "gzip");
            }
            else
            {
                AssertLog(logMessages.Skip(1).Single(), LogLevel.Debug, "No response compression available for HTTPS requests. See ResponseCompressionOptions.EnableForHttps.");
            }
        }

        [Theory]
        [InlineData(HttpsCompressionMode.Default, 100)]
        [InlineData(HttpsCompressionMode.DoNotCompress, 100)]
        [InlineData(HttpsCompressionMode.Compress, 30)]
        public async Task Request_Https_CompressedIfOptIn(HttpsCompressionMode mode, int expectedLength)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<ResponseCompressionProvider>,
                TestSink.EnableWithTypeName<ResponseCompressionProvider>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                    services.AddResponseCompression(options =>
                    {
                        options.EnableForHttps = false;
                        options.MimeTypes = new[] { TextPlain };
                    });
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        var feature = context.Features.Get<IHttpsCompressionFeature>();
                        feature.Mode = mode;
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

            var logMessages = sink.Writes.ToList();
            if (mode == HttpsCompressionMode.Compress)
            {
                AssertCompressedWithLog(logMessages, "gzip");
            }
            else
            {
                AssertLog(logMessages.Skip(1).Single(), LogLevel.Debug, "No response compression available for HTTPS requests. See ResponseCompressionOptions.EnableForHttps.");
            }
        }

        [Theory]
        [InlineData(HttpsCompressionMode.Default, 30)]
        [InlineData(HttpsCompressionMode.Compress, 30)]
        [InlineData(HttpsCompressionMode.DoNotCompress, 100)]
        public async Task Request_Https_NotCompressedIfOptOut(HttpsCompressionMode mode, int expectedLength)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<ResponseCompressionProvider>,
                TestSink.EnableWithTypeName<ResponseCompressionProvider>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                    services.AddResponseCompression(options =>
                    {
                        options.EnableForHttps = true;
                        options.MimeTypes = new[] { TextPlain };
                    });
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        var feature = context.Features.Get<IHttpsCompressionFeature>();
                        feature.Mode = mode;
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

            var logMessages = sink.Writes.ToList();
            if (mode == HttpsCompressionMode.DoNotCompress)
            {
                AssertLog(logMessages.Skip(1).Single(), LogLevel.Debug, "No response compression available for HTTPS requests. See ResponseCompressionOptions.EnableForHttps.");
            }
            else
            {
                AssertCompressedWithLog(logMessages, "gzip");
            }
        }

        [Theory]
        [MemberData(nameof(SupportedEncodingsWithBodyLength))]
        public async Task FlushHeaders_SendsHeaders_Compresses(string encoding, int expectedBodyLength)
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        context.Response.Body.Flush();
                        await responseReceived.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
                        await context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            responseReceived.SetResult(0);

            await response.Content.LoadIntoBufferAsync();

            CheckResponseCompressed(response, expectedBodyLength, encoding);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodingsWithBodyLength))]
        public async Task FlushAsyncHeaders_SendsHeaders_Compresses(string encoding, int expectedBodyLength)
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        await responseReceived.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
                        await context.Response.WriteAsync(new string('a', 100));
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd(encoding);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            responseReceived.SetResult(0);

            await response.Content.LoadIntoBufferAsync();

            CheckResponseCompressed(response, expectedBodyLength, encoding);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task FlushBody_CompressesAndFlushes(string encoding)
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        var feature = context.Features.Get<IHttpBodyControlFeature>();
                        if (feature != null)
                        {
                            feature.AllowSynchronousIO = true;
                        }

                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Response.Body.Write(new byte[10], 0, 10);
                        context.Response.Body.Flush();
                        await responseReceived.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
                        context.Response.Body.Write(new byte[90], 0, 90);
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

            responseReceived.SetResult(0);

            read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task FlushAsyncBody_CompressesAndFlushes(string encoding)
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        await responseReceived.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
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

            responseReceived.SetResult(0);

            read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task TrickleWriteAndFlush_FlushesEachWrite(string encoding)
        {
            var responseReceived = new[]
            {
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
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
                        context.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();

                        var feature = context.Features.Get<IHttpBodyControlFeature>();
                        if (feature != null)
                        {
                            feature.AllowSynchronousIO = true;
                        }

                        foreach (var signal in responseReceived)
                        {
                            context.Response.Body.Write(new byte[1], 0, 1);
                            context.Response.Body.Flush();
                            await signal.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
                        }
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

            foreach (var signal in responseReceived)
            {
                var read = await body.ReadAsync(new byte[100], 0, 100);
                Assert.True(read > 0);

                signal.SetResult(0);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task TrickleWriteAndFlushAsync_FlushesEachWrite(string encoding)
        {
            var responseReceived = new[]
            {
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
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
                        context.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();

                        foreach (var signal in responseReceived)
                        {
                            await context.Response.WriteAsync("a");
                            await context.Response.Body.FlushAsync();
                            await signal.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
                        }
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

            foreach (var signal in responseReceived)
            {
                var read = await body.ReadAsync(new byte[100], 0, 100);
                Assert.True(read > 0);

                signal.SetResult(0);
            }
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
                        fakeSendFile = new FakeSendFileFeature(context.Features.Get<IHttpResponseBodyFeature>());
                        context.Features.Set<IHttpResponseBodyFeature>(fakeSendFile);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = "custom/type";
                        context.Response.ContentLength = 1024;
                        var sendFile = context.Features.Get<IHttpResponseBodyFeature>();
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

            Assert.True(fakeSendFile.SendFileInvoked);
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
                        fakeSendFile = new FakeSendFileFeature(context.Features.Get<IHttpResponseBodyFeature>());
                        context.Features.Set<IHttpResponseBodyFeature>(fakeSendFile);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        context.Response.ContentLength = 1024;
                        var sendFile = context.Features.Get<IHttpResponseBodyFeature>();
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

            Assert.False(fakeSendFile.SendFileInvoked);
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
                        fakeSendFile = new FakeSendFileFeature(context.Features.Get<IHttpResponseBodyFeature>());
                        context.Features.Set<IHttpResponseBodyFeature>(fakeSendFile);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(async context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        var feature = context.Features.Get<IHttpResponseBodyFeature>();

                        await context.Response.WriteAsync(new string('a', 100));
                        await feature.SendFileAsync("testfile1kb.txt", 0, null, CancellationToken.None);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.AcceptEncoding.ParseAdd("gzip");

            var response = await client.SendAsync(request);

            CheckResponseCompressed(response, expectedBodyLength: 46, expectedEncoding: "gzip");

            Assert.False(fakeSendFile.SendFileInvoked);
        }

        [Theory]
        [MemberData(nameof(SupportedEncodings))]
        public async Task Dispose_SyncWriteOrFlushNotCalled(string encoding)
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression();
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Response.Body = new NoSyncWrapperStream(context.Response.Body);
                        return next();
                    });
                    app.UseResponseCompression();
                    app.Run(async context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = TextPlain;
                        await context.Response.WriteAsync(new string('a', 10));
                        await context.Response.Body.FlushAsync();
                        await responseReceived.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
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

            responseReceived.SetResult(0);

            read = await body.ReadAsync(new byte[100], 0, 100);
            Assert.True(read > 0);
        }

        private async Task<(HttpResponseMessage, List<WriteContext>)> InvokeMiddleware(
            int uncompressedBodyLength,
            string[] requestAcceptEncodings,
            string responseType,
            Action<HttpResponse> addResponseAction = null,
            Action<ResponseCompressionOptions> configure = null)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<ResponseCompressionProvider>,
                TestSink.EnableWithTypeName<ResponseCompressionProvider>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCompression(configure ?? (_ => { }));
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.UseResponseCompression();
                    app.Run(context =>
                    {
                        context.Response.Headers[HeaderNames.ContentMD5] = "MD5";
                        context.Response.ContentType = responseType;
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

            return (await client.SendAsync(request), sink.Writes.ToList());
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

        private void AssertLog(WriteContext log, LogLevel level, string message)
        {
            Assert.Equal(level, log.LogLevel);
            Assert.Equal(message, log.State.ToString());
        }

        private void AssertCompressedWithLog(List<WriteContext> logMessages, string provider)
        {
            Assert.Equal(3, logMessages.Count);
            AssertLog(logMessages.First(), LogLevel.Trace, "This request accepts compression.");
            AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, "Response compression is available for this Content-Type.");
            AssertLog(logMessages.Skip(2).First(), LogLevel.Debug, $"The response will be compressed with '{provider}'.");
        }

        private class FakeSendFileFeature : IHttpResponseBodyFeature
        {
            public FakeSendFileFeature(IHttpResponseBodyFeature innerFeature)
            {
                InnerFeature = innerFeature;
            }

            public IHttpResponseBodyFeature InnerFeature { get; }

            public bool SendFileInvoked { get; set; }

            public Stream Stream => InnerFeature.Stream;

            public PipeWriter Writer => InnerFeature.Writer;

            public Task CompleteAsync() => InnerFeature.CompleteAsync();

            public void DisableBuffering() => InnerFeature.DisableBuffering();

            public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
            {
                // This implementation should only be delegated to if compression is disabled.
                SendFileInvoked = true;
                return InnerFeature.SendFileAsync(path, offset, count, cancellation);
            }

            public Task StartAsync(CancellationToken token = default) => InnerFeature.StartAsync(token);
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

        private class NoSyncWrapperStream : Stream
        {
            private Stream _body;

            public NoSyncWrapperStream(Stream body)
            {
                _body = body;
            }

            public override bool CanRead => _body.CanRead;

            public override bool CanSeek => _body.CanSeek;

            public override bool CanWrite => _body.CanWrite;

            public override long Length => _body.Length;

            public override long Position
            {
                get => throw new InvalidOperationException("This shouldn't be called");
                set => throw new InvalidOperationException("This shouldn't be called");
            }

            public override void Flush()
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override void SetLength(long value)
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _body.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _body.WriteAsync(buffer, cancellationToken);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return _body.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                _body.EndWrite(asyncResult);
            }

            public override void Close()
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            protected override void Dispose(bool disposing)
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override ValueTask DisposeAsync()
            {
                return _body.DisposeAsync();
            }

            public override void CopyTo(Stream destination, int bufferSize)
            {
                throw new InvalidOperationException("This shouldn't be called");
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _body.FlushAsync(cancellationToken);
            }
        }
    }
}
