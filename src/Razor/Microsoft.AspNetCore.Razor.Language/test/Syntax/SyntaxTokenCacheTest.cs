// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

public class SyntaxTokenCacheTest
{
    // Regression test for https://github.com/dotnet/aspnetcore/issues/27154
    [Fact]
    public void GetCachedToken_ReturnsNewEntry()
    {
        // Arrange
        var cache = new SyntaxTokenCache();

        // Act
        var token = cache.GetCachedToken(SyntaxKind.Whitespace, "Hello world");

        // Assert
        Assert.Equal(SyntaxKind.Whitespace, token.Kind);
        Assert.Equal("Hello world", token.Content);
        Assert.Empty(token.GetDiagnostics());
    }

    [Fact]
    public void GetCachedToken_ReturnsCachedToken()
    {
        // Arrange
        var cache = new SyntaxTokenCache();

        // Act
        var token1 = cache.GetCachedToken(SyntaxKind.Whitespace, "Hello world");
        var token2 = cache.GetCachedToken(SyntaxKind.Whitespace, "Hello world");

        // Assert
        Assert.Same(token1, token2);
    }

    [Fact]
    public void GetCachedToken_ReturnsDifferentEntries_IfKindsAreDifferent()
    {
        // Arrange
        var cache = new SyntaxTokenCache();

        // Act
        var token1 = cache.GetCachedToken(SyntaxKind.Whitespace, "Hello world");
        var token2 = cache.GetCachedToken(SyntaxKind.Keyword, "Hello world");

        // Assert
        Assert.NotSame(token1, token2);
        Assert.Equal(SyntaxKind.Whitespace, token1.Kind);
        Assert.Equal("Hello world", token1.Content);

        Assert.Equal(SyntaxKind.Keyword, token2.Kind);
        Assert.Equal("Hello world", token2.Content);
    }

    [Fact]
    public void GetCachedToken_ReturnsDifferentEntries_IfContentsAreDifferent()
    {
        // Arrange
        var cache = new SyntaxTokenCache();

        // Act
        var token1 = cache.GetCachedToken(SyntaxKind.Keyword, "Text1");
        var token2 = cache.GetCachedToken(SyntaxKind.Keyword, "Text2");

        // Assert
        Assert.NotSame(token1, token2);
        Assert.Equal(SyntaxKind.Keyword, token1.Kind);
        Assert.Equal("Text1", token1.Content);

        Assert.Equal(SyntaxKind.Keyword, token2.Kind);
        Assert.Equal("Text2", token2.Content);
    }
}
