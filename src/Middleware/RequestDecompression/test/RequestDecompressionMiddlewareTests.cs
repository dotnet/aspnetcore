// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

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
        string input = TestRequestBodyData)
    {
        var bytes = GetUncompressedContent(input);
        await using var uncompressedContent = new MemoryStream(bytes);

        await using var compressedContent = new MemoryStream();
        await using (var compressor = compressorDelegate(compressedContent))
        {
            uncompressedContent.CopyTo(compressor);
        }

        return compressedContent.ToArray();
    }

    private static async Task<byte[]> GetBrotliCompressedContent(string input = TestRequestBodyData)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new BrotliStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, input);
    }

    private static async Task<byte[]> GetDeflateCompressedContent(string input = TestRequestBodyData)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new DeflateStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, input);
    }

    private static async Task<byte[]> GetGZipCompressedContent(string input = TestRequestBodyData)
    {
        static Stream compressorDelegate(Stream compressedContent) =>
            new GZipStream(compressedContent, CompressionMode.Compress);

        return await GetCompressedContent(compressorDelegate, input);
    }

    [Fact]
    public async Task Request_ContentEncodingBrotli_Decompressed()
    {
        var compressedContent = await GetBrotliCompressedContent();
        var contentEncoding = "br";

        var (logMessages, decompressedContent) = await InvokeMiddleware(compressedContent, new[] { contentEncoding });

        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(GetUncompressedContent(), decompressedContent);
    }

    [Fact]
    public async Task Request_ContentEncodingDeflate_Decompressed()
    {
        var compressedContent = await GetDeflateCompressedContent();
        var contentEncoding = "deflate";

        var (logMessages, decompressedContent) = await InvokeMiddleware(compressedContent, new[] { contentEncoding });

        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(GetUncompressedContent(), decompressedContent);
    }

    [Fact]
    public async Task Request_ContentEncodingGzip_Decompressed()
    {
        var compressedContent = await GetGZipCompressedContent();
        var contentEncoding = "gzip";

        var (logMessages, decompressedContent) = await InvokeMiddleware(compressedContent, new[] { contentEncoding });

        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(GetUncompressedContent(), decompressedContent);
    }

    [Fact]
    public async Task Request_NoContentEncoding_NotDecompressed()
    {
        var uncompressedContent = GetUncompressedContent();

        var (logMessages, outputContent) = await InvokeMiddleware(uncompressedContent);

        AssertLog(logMessages.Single(), LogLevel.Trace, "The Content-Encoding header is missing or empty. Skipping request decompression.");
        Assert.Equal(uncompressedContent, outputContent);
    }

    [Fact]
    public async Task Request_UnsupportedContentEncoding_NotDecompressed()
    {
        var inputContent = GetUncompressedContent();
        var contentEncoding = "custom";

        var (logMessages, outputContent) = await InvokeMiddleware(inputContent, new[] { contentEncoding });

        AssertLog(logMessages.First(), LogLevel.Debug, $"No matching request decompression provider found.");
        Assert.Equal(GetUncompressedContent(), outputContent);
    }

    [Fact]
    public async Task Request_MultipleContentEncodings_NotDecompressed()
    {
        var inputContent = GetUncompressedContent();
        var contentEncodings = new[] { "br", "gzip" };

        var (logMessages, outputContent) = await InvokeMiddleware(inputContent, contentEncodings);

        AssertLog(logMessages.First(), LogLevel.Debug, "Request decompression is not supported for multiple Content-Encodings.");
        Assert.Equal(GetUncompressedContent(), outputContent);
    }

    private static async Task<(List<WriteContext>, byte[])> InvokeMiddleware(
        byte[] compressedContent,
        string[] contentEncodings = null,
        Action<RequestDecompressionOptions> configure = null)
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<RequestDecompressionProvider>,
            TestSink.EnableWithTypeName<RequestDecompressionProvider>);
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

    [Fact]
    public async Task Request_MiddlewareAddedMultipleTimes_OnlyDecompressedOnce()
    {
        var compressedContent = await GetGZipCompressedContent();
        var contentEncoding = "gzip";

        var sink = new TestSink(
            TestSink.EnableWithTypeName<RequestDecompressionProvider>,
            TestSink.EnableWithTypeName<RequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var outputContent = Array.Empty<byte>();

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
                    app.UseRequestDecompression();
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
        request.Content.Headers.ContentEncoding.Add(contentEncoding);

        await client.SendAsync(request);

        var logMessages = sink.Writes.ToList();

        Assert.Equal(2, logMessages.Count);
        AssertLog(logMessages.First(), LogLevel.Debug, $"The request will be decompressed with '{contentEncoding}'.");
        AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, "The Content-Encoding header is missing or empty. Skipping request decompression.");
        Assert.Equal(GetUncompressedContent(), outputContent);
    }

    private static void AssertLog(WriteContext log, LogLevel level, string message)
    {
        Assert.Equal(level, log.LogLevel);
        Assert.Equal(message, log.State.ToString());
    }

    private static void AssertDecompressedWithLog(List<WriteContext> logMessages, string encoding)
    {
        var message = Assert.Single(logMessages);
        AssertLog(message, LogLevel.Debug, $"The request will be decompressed with '{encoding}'.");
    }
}
