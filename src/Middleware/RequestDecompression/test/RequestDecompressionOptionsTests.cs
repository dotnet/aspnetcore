// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression.Tests;

public class RequestDecompressionOptionsTests
{
    [Fact]
    public void Options_InitializedWithDefaultProviders()
    {
        // Arrange
        var defaultProviderCount = 3;

        // Act
        var options = new RequestDecompressionOptions();

        // Assert
        var providers = options.DecompressionProviders;
        Assert.Equal(defaultProviderCount, providers.Count);

        var brotliProvider = Assert.Contains("br", providers);
        Assert.IsType<BrotliDecompressionProvider>(brotliProvider);

        var deflateProvider = Assert.Contains("deflate", providers);
        Assert.IsType<DeflateDecompressionProvider>(deflateProvider);

        var gzipProvider = Assert.Contains("gzip", providers);
        Assert.IsType<GZipDecompressionProvider>(gzipProvider);
    }
}
