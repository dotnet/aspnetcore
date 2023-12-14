// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression.Tests;

public class DefaultRequestDecompressionProviderTests
{
    [Theory]
    [InlineData("br", typeof(BrotliStream))]
    [InlineData("BR", typeof(BrotliStream))]
    [InlineData("deflate", typeof(ZLibStream))]
    [InlineData("DEFLATE", typeof(ZLibStream))]
    [InlineData("gzip", typeof(GZipStream))]
    [InlineData("GZIP", typeof(GZipStream))]
    public void GetDecompressionProvider_SupportedContentEncoding_ReturnsProvider(
        string contentEncoding,
        Type expectedProviderType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add(HeaderNames.ContentEncoding, contentEncoding);

        var (logger, sink) = GetTestLogger();
        var options = Options.Create(new RequestDecompressionOptions());

        var provider = new DefaultRequestDecompressionProvider(logger, options);

        // Act
        var matchingProvider = provider.GetDecompressionStream(httpContext);

        // Assert
        Assert.NotNull(matchingProvider);
        Assert.IsType(expectedProviderType, matchingProvider);

        var logMessages = sink.Writes.ToList();
        AssertLog(logMessages.Single(), LogLevel.Debug,
            $"The request will be decompressed with '{contentEncoding.ToLowerInvariant()}'.");

        var contentEncodingHeader = httpContext.Request.Headers.ContentEncoding;
        Assert.Empty(contentEncodingHeader);
    }

    [Fact]
    public void GetDecompressionProvider_NoContentEncoding_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var (logger, sink) = GetTestLogger();
        var options = Options.Create(new RequestDecompressionOptions());

        var provider = new DefaultRequestDecompressionProvider(logger, options);

        // Act
        var matchingProvider = provider.GetDecompressionStream(httpContext);

        // Assert
        Assert.Null(matchingProvider);

        var logMessages = sink.Writes.ToList();
        AssertLog(logMessages.Single(), LogLevel.Trace,
            "The Content-Encoding header is empty or not specified. Skipping request decompression.");

        var contentEncodingHeader = httpContext.Request.Headers.ContentEncoding;
        Assert.Empty(contentEncodingHeader);
    }

    [Fact]
    public void GetDecompressionProvider_UnsupportedContentEncoding_ReturnsNull()
    {
        // Arrange
        var contentEncoding = "custom";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add(HeaderNames.ContentEncoding, contentEncoding);

        var (logger, sink) = GetTestLogger();
        var options = Options.Create(new RequestDecompressionOptions());

        var provider = new DefaultRequestDecompressionProvider(logger, options);

        // Act
        var matchingProvider = provider.GetDecompressionStream(httpContext);

        // Assert
        Assert.Null(matchingProvider);

        var logMessages = sink.Writes.ToList();
        AssertLog(logMessages.Single(),
            LogLevel.Debug, "No matching request decompression provider found.");

        var contentEncodingHeader = httpContext.Request.Headers.ContentEncoding;
        Assert.Equal(contentEncoding, contentEncodingHeader);
    }

    [Fact]
    public void GetDecompressionProvider_MultipleContentEncodings_ReturnsNull()
    {
        // Arrange
        var contentEncodings = new StringValues(new[] { "br", "gzip" });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add(HeaderNames.ContentEncoding, contentEncodings);

        var (logger, sink) = GetTestLogger();
        var options = Options.Create(new RequestDecompressionOptions());

        var provider = new DefaultRequestDecompressionProvider(logger, options);

        // Act
        var matchingProvider = provider.GetDecompressionStream(httpContext);

        // Assert
        Assert.Null(matchingProvider);

        var logMessages = sink.Writes.ToList();
        AssertLog(logMessages.Single(), LogLevel.Debug,
            "Request decompression is not supported for multiple Content-Encodings.");

        var contentEncodingHeader = httpContext.Request.Headers.ContentEncoding;
        Assert.Equal(contentEncodings, contentEncodingHeader);
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange
        var (logger, _) = GetTestLogger();
        IOptions<RequestDecompressionOptions> options = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new DefaultRequestDecompressionProvider(logger, options);
        });
    }

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        // Arrange
        ILogger<DefaultRequestDecompressionProvider> logger = null;
        var options = Options.Create(new RequestDecompressionOptions());

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new DefaultRequestDecompressionProvider(logger, options);
        });
    }

    private static (ILogger<DefaultRequestDecompressionProvider>, TestSink) GetTestLogger()
    {
        var sink = new TestSink(
           TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>,
           TestSink.EnableWithTypeName<DefaultRequestDecompressionProvider>);

        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = loggerFactory.CreateLogger<DefaultRequestDecompressionProvider>();

        return (logger, sink);
    }

    private static void AssertLog(WriteContext log, LogLevel level, string message)
    {
        Assert.Equal(level, log.LogLevel);
        Assert.Equal(message, log.State.ToString());
    }
}
