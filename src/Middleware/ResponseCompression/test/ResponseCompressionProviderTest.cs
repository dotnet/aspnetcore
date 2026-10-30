// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression.Tests;

public class ResponseCompressionProviderTest
{
    [Theory]
    [InlineData("gzip", "gzip")]
    [InlineData("gzip, br", "br")]
    [InlineData("gzip, br, zstd", "zstd")]
    [InlineData("gzip;q=0.5, br;q=0.8, gzip;q=1.0", "br")]
    [InlineData("br;q=0.5, gzip;q=0.6, BR;q=1.0", "gzip")]
    [InlineData("GZIP;q=0.9, br;q=0.8", "gzip")]
    [InlineData("gzip;q=0.5, identity;q=0.5", "gzip")]
    [InlineData("identity;q=0.5, gzip;q=0.5", "gzip")]
    [InlineData("identity, gzip;q=0.5", null)]
    [InlineData("identity", null)]
    [InlineData("gzip;q=0", null)]
    [InlineData("gzip;q=0, br;q=0.1", "br")]
    [InlineData("foo, bar", null)]
    [InlineData("*", "zstd")]
    [InlineData("*;q=0.5, gzip", "zstd")]
    [InlineData("gzip, *;q=0.1", "gzip")]
    [InlineData("br;q=0.2, *;q=0.5", "zstd")]
    [InlineData("*, identity", "zstd")]
    public void GetCompressionProvider_SelectsExpectedProvider(string acceptEncoding, string expectedEncoding)
    {
        var provider = CreateProvider();

        var selected = provider.GetCompressionProvider(CreateContext(acceptEncoding));

        Assert.Equal(expectedEncoding, selected?.EncodingName);
    }

    [Fact]
    public void GetCompressionProvider_DuplicateEncodingNames_FirstProviderWins()
    {
        var first = new TestCompressionProvider("custom");
        var second = new TestCompressionProvider("CUSTOM");
        var provider = CreateProvider(first, second);

        Assert.Same(first, provider.GetCompressionProvider(CreateContext("custom")));
        Assert.Same(first, provider.GetCompressionProvider(CreateContext("CUSTOM;q=0.5")));
        Assert.Same(first, provider.GetCompressionProvider(CreateContext("*")));
    }

    [Fact]
    public void GetCompressionProvider_ProviderNamedIdentity_IsSelectedForIdentity()
    {
        var identity = new TestCompressionProvider("identity");
        var provider = CreateProvider(new TestCompressionProvider("custom"), identity);

        Assert.Same(identity, provider.GetCompressionProvider(CreateContext("identity")));
        Assert.Same(identity, provider.GetCompressionProvider(CreateContext("identity, custom;q=0.5")));
    }

    private static ResponseCompressionProvider CreateProvider(params ICompressionProvider[] providers)
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddLogging()
            .AddResponseCompression()
            .BuildServiceProvider();

        var options = new ResponseCompressionOptions();
        foreach (var provider in providers)
        {
            options.Providers.Add(provider);
        }

        return new ResponseCompressionProvider(services, Options.Create(options));
    }

    private static HttpContext CreateContext(string acceptEncoding)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptEncoding = acceptEncoding;
        return context;
    }

    private sealed class TestCompressionProvider(string encodingName) : ICompressionProvider
    {
        public string EncodingName { get; } = encodingName;

        public bool SupportsFlush => true;

        public Stream CreateStream(Stream outputStream) => outputStream;
    }
}
