// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.RequestDecompression.Tests;

public class RequestDecompressionMiddlewareTests
{
    private const string TestRequestBodyData = "Test Request Body Data";

    private static byte[] GetUncompressedContent(string input = TestRequestBodyData)
    {
        return Encoding.UTF8.GetBytes(input);
    }

    private static async Task<byte[]> GetCompressedContent(
        Func<Stream, Stream> compressorDelegate,
        byte[] uncompressedBytes)
    {
        await using var uncompressedStream = new MemoryStream(uncompressedBytes);

        await using var compressedStream = new MemoryStream();
        await using (var compressor = compressorDelegate(compressedStream))
        {
            await uncompressedStream.CopyToAsync(compressor);
        }

        return compressedStream.ToArray();
    }

    private static async Task<byte[]> GetBrotliCompressedContent(byte[] uncompressedBytes)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new BrotliStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, uncompressedBytes);
    }

    private static async Task<byte[]> GetDeflateCompressedContent(byte[] uncompressedBytes)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new DeflateStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, uncompressedBytes);
    }

    private static async Task<byte[]> GetZlibCompressedContent(byte[] uncompressedBytes)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new ZLibStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, uncompressedBytes);
    }

    private static async Task<byte[]> GetGZipCompressedContent(byte[] uncompressedBytes)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new GZipStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, uncompressedBytes);
    }

    [Fact]
    public async Task Request_ContentEncodingBrotli_Decompressed()
    {
        // Arrange
        var contentEncoding = "br";
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetBrotliCompressedContent(uncompressedBytes);

        // Act
        var (logMessages, decompressedBytes) = await InvokeMiddleware(compressedBytes, new[] { contentEncoding });

        // Assert
        AssertDecompressedWithLog(logMessages, contentEncoding.ToLowerInvariant());
        Assert.Equal(uncompressedBytes, decompressedBytes);
    }

    [Fact]
    public async Task Request_ContentEncodingDeflate_ZlibCompressed_Decompressed()
    {
        // Arrange
        var contentEncoding = "deflate";
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetZlibCompressedContent(uncompressedBytes);

        // Act
        var (logMessages, decompressedBytes) = await InvokeMiddleware(compressedBytes, new[] { contentEncoding });

        // Assert
        AssertDecompressedWithLog(logMessages, contentEncoding.ToLowerInvariant());
        Assert.Equal(uncompressedBytes, decompressedBytes);
    }

    [Fact]
    public async Task Request_ContentEncodingDeflate_RawDeflateCompressed_Throws()
    {
        // This tests an incorrect version of deflate usage where the 'deflate'
        // content-encoding is used with raw, unwrapped deflate compression. This
        // usage doesn't conform to the spec (RFC 2616).

        // Arrange
        var contentEncoding = "deflate";
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetDeflateCompressedContent(uncompressedBytes);

        // Act/Assert
        await Assert.ThrowsAsync<InvalidDataException>(async () => await InvokeMiddleware(compressedBytes, new[] { contentEncoding }));
    }

    [Fact]
    public async Task Request_ContentEncodingGzip_Decompressed()
    {
        // Arrange
        var contentEncoding = "gzip";
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);

        // Act
        var (logMessages, decompressedBytes) = await InvokeMiddleware(compressedBytes, new[] { contentEncoding });

        // Assert
        AssertDecompressedWithLog(logMessages, contentEncoding.ToLowerInvariant());
        Assert.Equal(uncompressedBytes, decompressedBytes);
    }

    [Fact]
    public async Task Request_NoContentEncoding_NotDecompressed()
    {
        // Arrange
        var uncompressedBytes = GetUncompressedContent();

        // Act
        var (logMessages, outputBytes) = await InvokeMiddleware(uncompressedBytes);

        // Assert
        var logMessage = Assert.Single(logMessages);
        AssertLog(logMessage, LogLevel.Trace, "The Content-Encoding header is empty or not specified. Skipping request decompression.");
        Assert.Equal(uncompressedBytes, outputBytes);
    }

    [Fact]
    public async Task Request_UnsupportedContentEncoding_NotDecompressed()
    {
        // Arrange
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);
        var contentEncoding = "custom";

        // Act
        var (logMessages, outputBytes) = await InvokeMiddleware(compressedBytes, new[] { contentEncoding });

        // Assert
        AssertNoDecompressionProviderLog(logMessages);
        Assert.Equal(compressedBytes, outputBytes);
    }

    [Fact]
    public async Task Request_MultipleContentEncodings_NotDecompressed()
    {
        // Arrange
        var uncompressedBytes = GetUncompressedContent();
        var inputBytes = await GetGZipCompressedContent(uncompressedBytes);
        var contentEncodings = new[] { "br", "gzip" };

        // Act
        var (logMessages, outputBytes) = await InvokeMiddleware(inputBytes, contentEncodings);

        // Assert
        var logMessage = Assert.Single(logMessages);
        AssertLog(logMessage, LogLevel.Debug, "Request decompression is not supported for multiple Content-Encodings.");
        Assert.Equal(inputBytes, outputBytes);
    }

    [Fact]
    public async Task Request_MiddlewareAddedMultipleTimes_OnlyDecompressedOnce()
    {
        // Arrange
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);
        var contentEncoding = "gzip";

        var decompressedBytes = Array.Empty<byte>();

        var sink = new TestSink(
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRequestDecompression();
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Features.Set<IHttpMaxRequestBodySizeFeature>(
                            new FakeHttpMaxRequestBodySizeFeature());
                        return next(context);
                    });
                    app.UseRequestDecompression();
                    app.UseRequestDecompression();
                    app.Run(async context =>
                    {
                        await using var ms = new MemoryStream();
                        await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
                        decompressedBytes = ms.ToArray();
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedBytes);
        request.Content.Headers.ContentEncoding.Add(contentEncoding);

        // Act
        await client.SendAsync(request);

        // Assert
        var logMessages = sink.Writes.ToList();

        Assert.Equal(2, logMessages.Count);
        AssertLog(logMessages.First(), LogLevel.Debug, $"The request will be decompressed with '{contentEncoding}'.");
        AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, "The Content-Encoding header is empty or not specified. Skipping request decompression.");

        Assert.Equal(uncompressedBytes, decompressedBytes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Request_Decompressed_ContentEncodingHeaderRemoved(bool isDecompressed)
    {
        // Arrange
        var contentEncoding = isDecompressed ? "gzip" : "custom";
        var contentEncodingHeader = new StringValues();

        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);

        var outputBytes = Array.Empty<byte>();

        var sink = new TestSink(
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRequestDecompression();
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Features.Set<IHttpMaxRequestBodySizeFeature>(
                            new FakeHttpMaxRequestBodySizeFeature());
                        return next(context);
                    });
                    app.UseRequestDecompression();
                    app.Run(async context =>
                    {
                        contentEncodingHeader = context.Request.Headers.ContentEncoding;

                        await using var ms = new MemoryStream();
                        await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
                        outputBytes = ms.ToArray();
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedBytes);
        request.Content.Headers.ContentEncoding.Add(contentEncoding);

        // Act
        await client.SendAsync(request);

        // Assert
        var logMessages = sink.Writes.ToList();

        if (isDecompressed)
        {
            Assert.Empty(contentEncodingHeader);

            AssertDecompressedWithLog(logMessages, contentEncoding);
            Assert.Equal(uncompressedBytes, outputBytes);
        }
        else
        {
            Assert.Equal(contentEncoding, contentEncodingHeader);

            AssertNoDecompressionProviderLog(logMessages);
            Assert.Equal(compressedBytes, outputBytes);
        }
    }

    [Fact]
    public async Task Request_InvalidDataForContentEncoding_ThrowsInvalidOperationException()
    {
        // Arrange
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);
        var contentEncoding = "br";

        Exception exception = null;

        var sink = new TestSink(
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRequestDecompression();
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Features.Set<IHttpMaxRequestBodySizeFeature>(
                            new FakeHttpMaxRequestBodySizeFeature());
                        return next(context);
                    });
                    app.UseRequestDecompression();
                    app.Run(async context =>
                    {
                        exception = await Record.ExceptionAsync(async () =>
                        {
                            using var ms = new MemoryStream();
                            await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedBytes);
        request.Content.Headers.ContentEncoding.Add(contentEncoding);

        // Act
        await client.SendAsync(request);

        // Assert
        var logMessages = sink.Writes.ToList();

        AssertDecompressedWithLog(logMessages, contentEncoding.ToLowerInvariant());

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task Options_RegisterCustomDecompressionProvider()
    {
        // Arrange
        var uncompressedBytes = GetUncompressedContent();
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);
        var contentEncoding = "custom";

        // Act
        var (logMessages, decompressedBytes) =
            await InvokeMiddleware(
                compressedBytes,
                new[] { contentEncoding },
                configure: (RequestDecompressionOptions options) =>
                {
                    options.DecompressionProviders.Add(contentEncoding, new CustomDecompressionProvider());
                });

        // Assert
        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(uncompressedBytes, decompressedBytes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Endpoint_HasRequestSizeLimit_UsedForRequest(bool exceedsLimit)
    {
        // Arrange
        long attributeSizeLimit = 10;
        long featureSizeLimit = 5;

        var contentEncoding = "gzip";
        var uncompressedBytes = new byte[attributeSizeLimit + (exceedsLimit ? 1 : 0)];
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);

        var decompressedBytes = Array.Empty<byte>();
        Exception exception = null;

        var sink = new TestSink(
           TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
           TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRequestDecompression();
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Features.Set<IEndpointFeature>(
                            GetFakeEndpointFeature(attributeSizeLimit));
                        context.Features.Set<IHttpMaxRequestBodySizeFeature>(
                            new FakeHttpMaxRequestBodySizeFeature(featureSizeLimit));

                        return next(context);
                    });
                    app.UseRequestDecompression();
                    app.Run(async context =>
                    {
                        await using var ms = new MemoryStream();

                        exception = await Record.ExceptionAsync(async () =>
                        {
                            await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
                            decompressedBytes = ms.ToArray();
                        });

                        decompressedBytes = ms.ToArray();
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedBytes);
        request.Content.Headers.ContentEncoding.Add(contentEncoding);

        // Act
        await client.SendAsync(request);

        // Assert
        var logMessages = sink.Writes.ToList();
        AssertDecompressedWithLog(logMessages, contentEncoding);

        if (exceedsLimit)
        {
            Assert.NotNull(exception);
            Assert.IsAssignableFrom<InvalidOperationException>(exception);
            Assert.Equal("The maximum number of bytes have been read.", exception.Message);
        }
        else
        {
            Assert.Null(exception);
            Assert.Equal(uncompressedBytes, decompressedBytes);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Feature_HasRequestSizeLimit_UsedForRequest(bool exceedsLimit)
    {
        // Arrange
        long featureSizeLimit = 10;

        var contentEncoding = "gzip";
        var uncompressedBytes = new byte[featureSizeLimit + (exceedsLimit ? 1 : 0)];
        var compressedBytes = await GetGZipCompressedContent(uncompressedBytes);

        var decompressedBytes = Array.Empty<byte>();
        Exception exception = null;

        var sink = new TestSink(
           TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
           TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRequestDecompression();
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Features.Set<IHttpMaxRequestBodySizeFeature>(
                            new FakeHttpMaxRequestBodySizeFeature(featureSizeLimit));

                        return next(context);
                    });
                    app.UseRequestDecompression();
                    app.Run(async context =>
                    {
                        await using var ms = new MemoryStream();

                        exception = await Record.ExceptionAsync(async () =>
                        {
                            await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
                            decompressedBytes = ms.ToArray();
                        });

                        decompressedBytes = ms.ToArray();
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedBytes);
        request.Content.Headers.ContentEncoding.Add(contentEncoding);

        // Act
        await client.SendAsync(request);

        // Assert
        var logMessages = sink.Writes.ToList();
        AssertDecompressedWithLog(logMessages, contentEncoding);

        if (exceedsLimit)
        {
            Assert.NotNull(exception);
            Assert.IsAssignableFrom<InvalidOperationException>(exception);
            Assert.Equal("The maximum number of bytes have been read.", exception.Message);
        }
        else
        {
            Assert.Null(exception);
            Assert.Equal(uncompressedBytes, decompressedBytes);
        }
    }

    [Fact]
    public void Ctor_NullRequestDelegate_Throws()
    {
        // Arrange
        RequestDelegate requestDelegate = null;
        var logger = new TestLogger<RequestDecompressionMiddleware>(
            new TestLoggerFactory(new TestSink(), enabled: true));
        var provider = new FakeRequestDecompressionProvider();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new RequestDecompressionMiddleware(requestDelegate, logger, provider);
        });
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange
        static Task requestDelegate(HttpContext context) => Task.FromResult(context);
        ILogger<RequestDecompressionMiddleware> logger = null;
        var provider = new FakeRequestDecompressionProvider();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new RequestDecompressionMiddleware(requestDelegate, logger, provider);
        });
    }

    [Fact]
    public void Ctor_NullRequestDecompressionProvider_Throws()
    {
        // Arrange
        static Task requestDelegate(HttpContext context) => Task.FromResult(context);
        var logger = new TestLogger<RequestDecompressionMiddleware>(
            new TestLoggerFactory(new TestSink(), enabled: true));
        IRequestDecompressionProvider provider = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new RequestDecompressionMiddleware(requestDelegate, logger, provider);
        });
    }

    private class FakeRequestDecompressionProvider : IRequestDecompressionProvider
    {
        private readonly bool _isCompressed;

        public FakeRequestDecompressionProvider(bool isCompressed = false)
        {
            _isCompressed = isCompressed;
        }

#nullable enable
        public Stream? GetDecompressionStream(HttpContext context)
            => _isCompressed
                ? new MemoryStream()
                : null;
#nullable disable
    }

    private static void AssertLog(WriteContext log, LogLevel level, string message)
    {
        Assert.Equal(level, log.LogLevel);
        Assert.Equal(message, log.State.ToString());
    }

    private static void AssertDecompressedWithLog(List<WriteContext> logMessages, string encoding)
    {
        var logMessage = Assert.Single(logMessages);
        AssertLog(logMessage, LogLevel.Debug, $"The request will be decompressed with '{encoding}'.");
    }

    private static void AssertNoDecompressionProviderLog(List<WriteContext> logMessages)
    {
        var logMessage = Assert.Single(logMessages);
        AssertLog(logMessage, LogLevel.Debug, "No matching request decompression provider found.");
    }

    private static async Task<(List<WriteContext>, byte[])> InvokeMiddleware(
        byte[] compressedContent,
        string[] contentEncodings = null,
        Action<RequestDecompressionOptions> configure = null)
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
            TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var outputContent = Array.Empty<byte>();

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRequestDecompression(configure ?? (_ => { }));
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                })
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Features.Set<IHttpMaxRequestBodySizeFeature>(
                            new FakeHttpMaxRequestBodySizeFeature());
                        return next(context);
                    });
                    app.UseRequestDecompression();
                    app.Run(async context =>
                    {
                        await using var ms = new MemoryStream();
                        await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
                        outputContent = ms.ToArray();
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedContent);

        if (contentEncodings != null)
        {
            foreach (var encoding in contentEncodings)
            {
                request.Content.Headers.ContentEncoding.Add(encoding);
            }
        }

        await client.SendAsync(request);

        return (sink.Writes.ToList(), outputContent);
    }
    private class CustomDecompressionProvider : IDecompressionProvider
    {
        public Stream GetDecompressionStream(Stream stream)
        {
            return new GZipStream(stream, CompressionMode.Decompress);
        }
    }

    private static FakeEndpointFeature GetFakeEndpointFeature(long? requestSizeLimit)
    {
        var requestSizeLimitMetadata = new FakeRequestSizeLimitMetadata
        {
            MaxRequestBodySize = requestSizeLimit
        };

        var endpointMetadata =
            new EndpointMetadataCollection(new[] { requestSizeLimitMetadata });

        return new FakeEndpointFeature
        {
            Endpoint = new Endpoint(
                requestDelegate: null,
                metadata: endpointMetadata,
                displayName: null)
        };
    }

    private class FakeEndpointFeature : IEndpointFeature
    {
        public Endpoint Endpoint { get; set; }
    }

    private class FakeRequestSizeLimitMetadata : IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize { get; set; }
    }

    private class FakeHttpMaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public FakeHttpMaxRequestBodySizeFeature(
            long? maxRequestBodySize = null,
            bool isReadOnly = false)
        {
            MaxRequestBodySize = maxRequestBodySize;
            IsReadOnly = isReadOnly;
        }

        public bool IsReadOnly { get; }

        public long? MaxRequestBodySize { get; set; }
    }
}
