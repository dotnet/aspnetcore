// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

public class PrefixResolverTests
{
    [Fact]
    public void HasPrefix_ReturnsTrue_WhenPrefixMatches()
    {
        // Arrange
        var prefixResolver = new PrefixResolver(GetKeys("foo"), 1);

        // Act
        var result = prefixResolver.HasPrefix("foo".AsMemory());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsPrefix_EmptyCollection_EmptyString_False()
    {
        // Arrange
        var prefixResolver = new PrefixResolver(Array.Empty<FormKey>(), 1);

        // Act
        var result = prefixResolver.HasPrefix(string.Empty.AsMemory());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsPrefix_HasEntries_EmptyString_True()
    {
        // Arrange
        var keys = new string[] { "some.prefix" };
        var prefixResolver = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = prefixResolver.HasPrefix(string.Empty.AsMemory());
        Assert.True(result);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("bc")]
    [InlineData("d")]
    public void ContainsPrefix_ReturnsTrue_IfTheContainerHasAnExactMatch(string prefix)
    {
        // Arrange
        var keys = new string[] { "bc", "a", "abc", "d" };
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix(prefix.AsMemory());

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("b")]
    [InlineData("c")]
    [InlineData("d")]
    public void ContainsPrefix_HasEntries_NoMatch(string prefix)
    {
        // Arrange
        var keys = new string[] { "ax", "bx", "cx", "dx" };
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix(prefix.AsMemory());

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("b")]
    [InlineData("b.xy")]
    [InlineData("c")]
    [InlineData("c.x")]
    [InlineData("c.x.y")]
    [InlineData("d")]
    [InlineData("d.x")]
    [InlineData("d.x.z")]
    public void ContainsPrefix_HasEntries_PrefixMatch_WithDot(string prefix)
    {
        // Arrange
        var keys = new string[] { "a.x", "b.xy", "c.x.y", "d.x.z[0]" };
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix(prefix.AsMemory());

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a.b.c")]
    [InlineData("a.b[1]")]
    [InlineData("a.b0")]
    public void ContainsPrefix_ReturnsFalse_IfPrefixDoesNotMatch(string prefix)
    {
        // Arrange
        var keys = new string[] { "a.b", "a.bc", "a.b[c]", "a.b[0]" };
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix(prefix.AsMemory());

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a[x]")]
    [InlineData("d[x]")]
    [InlineData("d[x].y")]
    [InlineData("e")]
    [InlineData("e.a.b")]
    [InlineData("e.a.b[foo]")]
    [InlineData("e.a.b[foo].bar")]
    public void ContainsPrefix_HasEntries_PrefixMatch_WithSquareBrace(string prefix)
    {
        // Arrange
        var keys = new string[] { "a[x]", "d[x].y", "e.a.b[foo].bar" };
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix(prefix.AsMemory());

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ContainsPrefix_HasEntries_PartialAndPrefixMatch_WithDot(int partialMatches)
    {
        // Arrange
        var keys = new string[partialMatches + 1];
        for (var i = 0; i < partialMatches; i++)
        {
            keys[i] = $"aa[{i}]";
        }
        keys[partialMatches] = "a.b"; // Sorted before all "aa" keys.
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix("a".AsMemory());

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ContainsPrefix_HasEntries_PartialAndPrefixMatch_WithSquareBrace(int partialMatches)
    {
        // Arrange
        var keys = new string[partialMatches + 1];
        for (var i = 0; i < partialMatches; i++)
        {
            keys[i] = $"aa[{i}]";
        }
        keys[partialMatches] = "a[0]"; // Sorted after all "aa" keys.
        var container = new PrefixResolver(GetKeys(keys), keys.Length);

        // Act
        var result = container.HasPrefix("a".AsMemory());

        // Assert
        Assert.True(result);
    }

    private static IEnumerable<FormKey> GetKeys(params string[] keys)
    {
        return keys.Select(k => new FormKey(k.AsMemory()));
    }
}
