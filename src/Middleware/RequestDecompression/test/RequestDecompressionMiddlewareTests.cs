// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Net;
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
    public async Task Request_NoContentEncoding_NotDecompressed()
    {
        var uncompressedContent = GetUncompressedContent();

        var (logMessages, outputContent) = await InvokeMiddleware(uncompressedContent);

        AssertLog(logMessages.Single(), LogLevel.Trace, "The Content-Encoding header is missing or empty. Skipping request decompression.");
        Assert.Equal(uncompressedContent, outputContent);
    }

    [Fact]
    public async Task Request_ContentEncodingBrotli_Decompressed()
    {
        var compressedContent = await GetBrotliCompressedContent();
        var contentEncoding = "br";

        var (logMessages, decompressedContent) = await InvokeMiddleware(compressedContent, contentEncoding);

        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(GetUncompressedContent(), decompressedContent);
    }

    [Fact]
    public async Task Request_ContentEncodingDeflate_Decompressed()
    {
        var compressedContent = await GetDeflateCompressedContent();
        var contentEncoding = "deflate";

        var (logMessages, decompressedContent) = await InvokeMiddleware(compressedContent, contentEncoding);

        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(GetUncompressedContent(), decompressedContent);
    }

    [Fact]
    public async Task Request_ContentEncodingGzip_Decompressed()
    {
        var compressedContent = await GetGZipCompressedContent();
        var contentEncoding = "gzip";

        var (logMessages, decompressedContent) = await InvokeMiddleware(compressedContent, contentEncoding);

        AssertDecompressedWithLog(logMessages, contentEncoding);
        Assert.Equal(GetUncompressedContent(), decompressedContent);
    }

    [Fact]
    public async Task Request_UnsupportedContentEncoding_Returns415UnsupportedMediaType()
    {
        var contentEncoding = "custom";

        var (logMessages, response) = await InvokeMiddleware(contentEncoding);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        AssertLog(logMessages.First(), LogLevel.Trace, "The Content-Encoding header is specified. Proceeding with request decompression.");
        AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, $"Request decompression is not supported for Content-Encoding '{contentEncoding}'.");
    }

    [Fact]
    public async Task Request_MultipleContentEncodings_Returns415UnsupportedMediaType()
    {
        var contentEncodings = new[] { "br", "gzip" };

        var (logMessages, response) = await InvokeMiddleware(contentEncodings);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        AssertLog(logMessages.First(), LogLevel.Trace, "The Content-Encoding header is specified. Proceeding with request decompression.");
        AssertLog(logMessages.Skip(1).First(), LogLevel.Debug, "Request decompression is not supported for multiple Content-Encodings.");
    }

    private static async Task<(List<WriteContext>, byte[])> InvokeMiddleware(
        byte[] compressedContent,
        string contentEncoding = null,
        Action<RequestDecompressionOptions> configure = null)
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<RequestDecompressionProvider>,
            TestSink.EnableWithTypeName<RequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var decompressedContent = Array.Empty<byte>();

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
                        decompressedContent = ms.ToArray();
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(compressedContent);

        if (contentEncoding != null)
        {
            request.Content.Headers.ContentEncoding.Add(contentEncoding);
        }

        await client.SendAsync(request);

        return (sink.Writes.ToList(), decompressedContent);
    }

    private static async Task<(List<WriteContext>, HttpResponseMessage)> InvokeMiddleware(
        string contentEncoding,
        Action<RequestDecompressionOptions> configure = null)
    {
        return await InvokeMiddleware(new[] { contentEncoding }, configure);
    }

    private static async Task<(List<WriteContext>, HttpResponseMessage)> InvokeMiddleware(
        string[] contentEncodings,
        Action<RequestDecompressionOptions> configure = null)
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<RequestDecompressionProvider>,
            TestSink.EnableWithTypeName<RequestDecompressionProvider>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

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
                    app.Run(async context => await Task.CompletedTask);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = new ByteArrayContent(new byte[10]);

        foreach (var encoding in contentEncodings)
        {
            request.Content.Headers.ContentEncoding.Add(encoding);
        }

        var response = await client.SendAsync(request);

        return (sink.Writes.ToList(), response);
    }

    private static void AssertLog(WriteContext log, LogLevel level, string message)
    {
        Assert.Equal(level, log.LogLevel);
        Assert.Equal(message, log.State.ToString());
    }

    private static void AssertDecompressedWithLog(List<WriteContext> logMessages, string encoding)
    {
        Assert.Equal(3, logMessages.Count);
        AssertLog(logMessages.First(), LogLevel.Trace, "The Content-Encoding header is specified. Proceeding with request decompression.");
        AssertLog(logMessages.Skip(1).First(), LogLevel.Trace, $"Request decompression is supported for Content-Encoding '{encoding}'.");
        AssertLog(logMessages.Skip(2).First(), LogLevel.Debug, $"The request will be decompressed with '{encoding}'.");
    }
}
