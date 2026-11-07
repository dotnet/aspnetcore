// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.StaticFiles;

public class DefaultContentTypeProviderTests
{
    [Fact]
    public void UnknownExtensionsReturnFalse()
    {
        var provider = new FileExtensionContentTypeProvider();
        string contentType;
        Assert.False(provider.TryGetContentType("unknown.ext", out contentType));
    }

    [Fact]
    public void KnownExtensionsReturnType()
    {
        var provider = new FileExtensionContentTypeProvider();
        Assert.True(provider.TryGetContentType("known.txt", out var contentType));
        Assert.Equal("text/plain", contentType);
    }

    [Fact]
    public void DoubleDottedExtensionsAreNotSupported()
    {
        var provider = new FileExtensionContentTypeProvider();
        string contentType;
        Assert.False(provider.TryGetContentType("known.exe.config", out contentType));
    }

    [Fact]
    public void DashedExtensionsShouldBeMatched()
    {
        var provider = new FileExtensionContentTypeProvider();
        Assert.True(provider.TryGetContentType("known.dvr-ms", out var contentType));
        Assert.Equal("video/x-ms-dvr", contentType);
    }

    [Fact]
    public void BothSlashFormatsAreUnderstood()
    {
        var provider = new FileExtensionContentTypeProvider();
        Assert.True(provider.TryGetContentType(@"/first/example.txt", out var contentType));
        Assert.Equal("text/plain", contentType);
        Assert.True(provider.TryGetContentType(@"\second\example.txt", out contentType));
        Assert.Equal("text/plain", contentType);
    }

    [Fact]
    public void DotsInDirectoryAreIgnored()
    {
        var provider = new FileExtensionContentTypeProvider();
        Assert.True(provider.TryGetContentType(@"/first.css/example.txt", out var contentType));
        Assert.Equal("text/plain", contentType);
        Assert.True(provider.TryGetContentType(@"\second.css\example.txt", out contentType));
        Assert.Equal("text/plain", contentType);
    }

    [Fact]
    public void InvalidCharactersAreIgnored()
    {
        var provider = new FileExtensionContentTypeProvider();
        string contentType;
        Assert.True(provider.TryGetContentType($"{new string(System.IO.Path.GetInvalidPathChars())}.txt", out contentType));
    }

    [Fact]
    public void CustomCaseSensitiveMappingIsRespected()
    {
        var provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { ".txt", "text/custom" },
        });

        Assert.True(provider.TryGetContentType("file.txt", out var contentType));
        Assert.Equal("text/custom", contentType);
        Assert.False(provider.TryGetContentType("file.TXT", out _));
    }

    [Fact]
    public void ChangesToMappingsAfterConstructionAreObserved()
    {
        var provider = new FileExtensionContentTypeProvider();

        Assert.False(provider.TryGetContentType("file.custom", out _));
        provider.Mappings[".custom"] = "application/x-custom";
        Assert.True(provider.TryGetContentType("file.CUSTOM", out var contentType));
        Assert.Equal("application/x-custom", contentType);

        provider.Mappings.Remove(".txt");
        Assert.False(provider.TryGetContentType("file.txt", out _));
    }

    [Fact]
    public void TrailingDotLooksUpDotKey()
    {
        var provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".", "application/x-dot" },
        });

        Assert.True(provider.TryGetContentType("file.", out var contentType));
        Assert.Equal("application/x-dot", contentType);
    }

    [Fact]
    public void NonDictionaryMappingIsUsed()
    {
        var provider = new FileExtensionContentTypeProvider(new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".txt", "text/custom" },
        });

        Assert.True(provider.TryGetContentType("file.TXT", out var contentType));
        Assert.Equal("text/custom", contentType);
    }

    [Fact]
    public void DerivedDictionaryTryGetValueIsUsed()
    {
        var mapping = new InterceptingDictionary { { ".txt", "text/plain" } };
        var provider = new FileExtensionContentTypeProvider(mapping);

        Assert.True(provider.TryGetContentType("file.txt", out var contentType));
        Assert.Equal("intercepted", contentType);
        Assert.Equal(".txt", Assert.Single(mapping.RequestedKeys));
    }

    private sealed class InterceptingDictionary : Dictionary<string, string>, IDictionary<string, string>
    {
        public List<string> RequestedKeys { get; } = new();

        bool IDictionary<string, string>.TryGetValue(string key, out string value)
        {
            RequestedKeys.Add(key);
            value = "intercepted";
            return true;
        }
    }
}
