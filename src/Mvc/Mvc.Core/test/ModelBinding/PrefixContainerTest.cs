// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class PrefixContainerTest
{
    [Fact]
    public void ContainsPrefix_EmptyCollection_EmptyString_False()
    {
        // Arrange
        var keys = new string[] { };
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsPrefix_HasEntries_EmptyString_True()
    {
        // Arrange
        var keys = new string[] { "some.prefix" };
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(string.Empty);

        // Assert
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
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(prefix);

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
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(prefix);

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
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(prefix);

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
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(prefix);

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
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix(prefix);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    public void ContainsPrefix_HasEntries_PartialAndPrefixMatch_WithDot(int partialMatches)
    {
        // Arrange
        var keys = new string[partialMatches + 1];
        for (var i = 0; i < partialMatches; i++)
        {
            keys[i] = $"aa[{i}]";
        }
        keys[partialMatches] = "a.b"; // Sorted before all "aa" keys.
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix("a");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    public void ContainsPrefix_HasEntries_PartialAndPrefixMatch_WithSquareBrace(int partialMatches)
    {
        // Arrange
        var keys = new string[partialMatches + 1];
        for (var i = 0; i < partialMatches; i++)
        {
            keys[i] = $"aa[{i}]";
        }
        keys[partialMatches] = "a[0]"; // Sorted after all "aa" keys.
        var container = new PrefixContainer(keys);

        // Act
        var result = container.ContainsPrefix("a");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    [InlineData(16)]
    public void ContainsPrefix_MatchesExpected_ForSmallAndLargeCollections(int fillerCount)
    {
        // Arrange
        // Small collections are searched without sorting and large ones with a binary search; both must agree.
        // "OrderId" and "productId" are partial matches that sort next to the delimited keys, so the sorted path must fall
        // back from its binary search hit to find "Order.Lines[2].Sku" and "product[0]".
        var keys = new List<string> { "Zeta", "productId", "product[0]", "OrderId", "Order.Lines[2].Sku", null, "[key]", "ab" };
        for (var i = 0; i < fillerCount; i++)
        {
            keys.Insert(i % keys.Count, "filler" + i);
        }

        var container = new PrefixContainer(keys);

        // Act & Assert
        Assert.True(container.ContainsPrefix(""));
        Assert.True(container.ContainsPrefix("zeta"));
        Assert.True(container.ContainsPrefix("ZETA"));
        Assert.True(container.ContainsPrefix("productid"));
        Assert.True(container.ContainsPrefix("product"));
        Assert.True(container.ContainsPrefix("PRODUCT[0]"));
        Assert.True(container.ContainsPrefix("order"));
        Assert.True(container.ContainsPrefix("order.lines"));
        Assert.True(container.ContainsPrefix("Order.Lines[2]"));
        Assert.True(container.ContainsPrefix("order.lines[2].sku"));
        Assert.True(container.ContainsPrefix("[key]"));
        Assert.Equal(fillerCount > 0, container.ContainsPrefix("filler0"));

        Assert.False(container.ContainsPrefix("Zet"));
        Assert.False(container.ContainsPrefix("prod"));
        Assert.False(container.ContainsPrefix("Order.Line"));
        Assert.False(container.ContainsPrefix("Order.Lines[2].Sk"));
        Assert.False(container.ContainsPrefix("a"));
        Assert.False(container.ContainsPrefix("abc"));
        Assert.False(container.ContainsPrefix("[ke"));
        Assert.False(container.ContainsPrefix("filler"));
        Assert.False(container.ContainsPrefix("missing"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(20)]
    public void ContainsPrefix_UsesSnapshotOfValues(int count)
    {
        // Arrange
        var keys = new List<string>();
        for (var i = 0; i < count; i++)
        {
            keys.Add("key" + i);
        }

        var container = new PrefixContainer(keys);

        // Act
        keys.Clear();
        keys.Add("other");

        // Assert
        Assert.True(container.ContainsPrefix("key0"));
        Assert.False(container.ContainsPrefix("other"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("foo")]
    public void GetKeysFromPrefix_ReturnsEmptySequenceWhenContainerIsEmpty(string prefix)
    {
        // Arrange
        var keys = new string[0];
        var container = new PrefixContainer(keys);

        // Act
        var result = container.GetKeysFromPrefix(prefix);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetKeysFromPrefix_ReturnsUniqueTopLevelEntries_WhenPrefixIsEmpty()
    {
        // Arrange
        var keys = new[] { "[0].name", "[0].address.street", "[item1].name", "[item1].age", "foo", "foo.bar" };
        var container = new PrefixContainer(keys);

        // Act
        var result = container.GetKeysFromPrefix(prefix: string.Empty);

        // Assert
        Assert.Collection(result.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase),
            item =>
            {
                Assert.Equal("0", item.Key);
                Assert.Equal("[0]", item.Value);
            },
            item =>
            {
                Assert.Equal("foo", item.Key);
                Assert.Equal("foo", item.Value);
            },
            item =>
            {
                Assert.Equal("item1", item.Key);
                Assert.Equal("[item1]", item.Value);
            });
    }

    [Fact]
    public void GetKeysFromPrefix_ReturnsEmptyDictionaryWhenNoKeysStartWithPrefix()
    {
        // Arrange
        var keys = new[] { "foo[0].name", "foo.age", "[1].name", "[item].age" };
        var container = new PrefixContainer(keys);

        // Act
        var result = container.GetKeysFromPrefix("baz");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetKeysFromPrefix_ReturnsSubKeysThatStartWithPrefix()
    {
        // Arrange
        var keys = new[] { "foo[0].name", "foo.age", "foo[1].name", "food[item].spice", "foo.name.first" };
        var container = new PrefixContainer(keys);

        // Act
        var result = container.GetKeysFromPrefix("foo");

        // Assert
        Assert.Collection(result.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase),
            item =>
            {
                Assert.Equal("0", item.Key);
                Assert.Equal("foo[0]", item.Value);
            },
            item =>
            {
                Assert.Equal("1", item.Key);
                Assert.Equal("foo[1]", item.Value);
            },
            item =>
            {
                Assert.Equal("age", item.Key);
                Assert.Equal("foo.age", item.Value);
            },
            item =>
            {
                Assert.Equal("name", item.Key);
                Assert.Equal("foo.name", item.Value);
            });
    }

    [Fact]
    public void GetKeysFromPrefix_ReturnsSubKeysThatStartWithPrefix_ForNestedSubKeys()
    {
        // Arrange
        var keys = new[] { "person[0].address[0].street", "person[0].address[1].street", "person[0].address[1].zip" };
        var container = new PrefixContainer(keys);

        // Act
        var result = container.GetKeysFromPrefix("person[0].address");

        // Assert
        Assert.Collection(result.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase),
            item =>
            {
                Assert.Equal("0", item.Key);
                Assert.Equal("person[0].address[0]", item.Value);
            },
            item =>
            {
                Assert.Equal("1", item.Key);
                Assert.Equal("person[0].address[1]", item.Value);
            });
    }
}
