// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Routing.Matching;

// We get a lot of good coverage of basics since this implementation is used
// as the default in many cases. The tests here are focused on details of the
// implementation (boundaries, casing, non-ASCII).
public abstract class ILEmitTreeJumpTableTestBase : MultipleEntryJumpTableTest
{
    public abstract bool Vectorize { get; }

    internal override JumpTable CreateTable(
        int defaultDestination,
        int exitDestination,
        params (string text, int destination)[] entries)
    {
        var fallback = new DictionaryJumpTable(defaultDestination, exitDestination, entries);
        var table = new ILEmitTrieJumpTable(defaultDestination, exitDestination, entries, Vectorize, fallback);
        table.InitializeILDelegate();
        return table;
    }

    [Fact] // Not calling CreateTable here because we want to test the initialization
    public async Task InitializeILDelegateAsync_ReplacesDelegate()
    {
        // Arrange
        var table = new ILEmitTrieJumpTable(0, -1, new[] { ("hi", 1), }, Vectorize, Mock.Of<JumpTable>());
        var original = table._getDestination;

        // Act
        await table.InitializeILDelegateAsync();

        // Assert
        Assert.NotSame(original, table._getDestination);
    }

    // Tests that we can detect non-ASCII characters and use the fallback jump table.
    // Testing different indices since that affects which part of the code is running.
    // \u007F = lowest non-ASCII character
    // \uFFFF = highest non-ASCII character
    [Theory]

    // non-ASCII character in first section non-vectorized comparisons
    [InlineData("he\u007F", "he\u007Flo-world", 0, 3)]
    [InlineData("he\uFFFF", "he\uFFFFlo-world", 0, 3)]
    [InlineData("e\u007F", "he\u007Flo-world", 1, 2)]
    [InlineData("e\uFFFF", "he\uFFFFlo-world", 1, 2)]
    [InlineData("\u007F", "he\u007Flo-world", 2, 1)]
    [InlineData("\uFFFF", "he\uFFFFlo-world", 2, 1)]

    // non-ASCII character in first section vectorized comparisons
    [InlineData("hel\u007F", "hel\u007Fo-world", 0, 4)]
    [InlineData("hel\uFFFF", "hel\uFFFFo-world", 0, 4)]
    [InlineData("el\u007Fo", "hel\u007Fo-world", 1, 4)]
    [InlineData("el\uFFFFo", "hel\uFFFFo-world", 1, 4)]
    [InlineData("l\u007Fo-", "hel\u007Fo-world", 2, 4)]
    [InlineData("l\uFFFFo-", "hel\uFFFFo-world", 2, 4)]
    [InlineData("\u007Fo-w", "hel\u007Fo-world", 3, 4)]
    [InlineData("\uFFFFo-w", "hel\uFFFFo-world", 3, 4)]

    // non-ASCII character in second section non-vectorized comparisons
    [InlineData("hello-\u007F", "hello-\u007Forld", 0, 7)]
    [InlineData("hello-\uFFFF", "hello-\uFFFForld", 0, 7)]
    [InlineData("ello-\u007F", "hello-\u007Forld", 1, 6)]
    [InlineData("ello-\uFFFF", "hello-\uFFFForld", 1, 6)]
    [InlineData("llo-\u007F", "hello-\u007Forld", 2, 5)]
    [InlineData("llo-\uFFFF", "hello-\uFFFFForld", 2, 5)]

    // non-ASCII character in first section vectorized comparisons
    [InlineData("hello-w\u007F", "hello-w\u007Forld", 0, 8)]
    [InlineData("hello-w\uFFFF", "hello-w\uFFFForld", 0, 8)]
    [InlineData("ello-w\u007Fo", "hello-w\u007Forld", 1, 8)]
    [InlineData("ello-w\uFFFFo", "hello-w\uFFFForld", 1, 8)]
    [InlineData("llo-w\u007For", "hello-w\u007Forld", 2, 8)]
    [InlineData("llo-w\uFFFFor", "hello-w\uFFFForld", 2, 8)]
    [InlineData("lo-w\u007Forl", "hello-w\u007Forld", 3, 8)]
    [InlineData("lo-w\uFFFForl", "hello-w\uFFFForld", 3, 8)]
    public void GetDestination_Found_IncludesNonAsciiCharacters(string entry, string path, int start, int length)
    {
        // Makes it easy to spot invalid tests
        Assert.Equal(entry.Length, length);
        Assert.Equal(entry, path.Substring(start, length), ignoreCase: true);

        // Arrange
        var table = CreateTable(0, -1, new[] { (entry, 1), });

        var segment = new PathSegment(start, length);

        // Act
        var result = table.GetDestination(path, segment);

        // Assert
        Assert.Equal(1, result);
    }

    // Tests for difference in casing with ASCII casing rules. Verifies our case
    // manipulation algorithm is correct.
    //
    // We convert from upper case to lower
    // 'A' and 'a' are 32 bits apart at the low end
    // 'Z' and 'z' are 32 bits apart at the high end
    [Theory]

    // character in first section non-vectorized comparisons
    [InlineData("heA", "healo-world", 0, 3)]
    [InlineData("heZ", "hezlo-world", 0, 3)]
    [InlineData("eA", "healo-world", 1, 2)]
    [InlineData("eZ", "hezlo-world", 1, 2)]
    [InlineData("A", "healo-world", 2, 1)]
    [InlineData("Z", "hezlo-world", 2, 1)]

    // character in first section vectorized comparisons
    [InlineData("helA", "helao-world", 0, 4)]
    [InlineData("helZ", "helzo-world", 0, 4)]
    [InlineData("elAo", "helao-world", 1, 4)]
    [InlineData("elZo", "helzo-world", 1, 4)]
    [InlineData("lAo-", "helao-world", 2, 4)]
    [InlineData("lZo-", "helzo-world", 2, 4)]
    [InlineData("Ao-w", "helao-world", 3, 4)]
    [InlineData("Zo-w", "helzo-world", 3, 4)]

    // character in second section non-vectorized comparisons
    [InlineData("hello-A", "hello-aorld", 0, 7)]
    [InlineData("hello-Z", "hello-zorld", 0, 7)]
    [InlineData("ello-A", "hello-aorld", 1, 6)]
    [InlineData("ello-Z", "hello-zorld", 1, 6)]
    [InlineData("llo-A", "hello-aorld", 2, 5)]
    [InlineData("llo-Z", "hello-zForld", 2, 5)]

    // character in first section vectorized comparisons
    [InlineData("hello-wA", "hello-waorld", 0, 8)]
    [InlineData("hello-wZ", "hello-wzorld", 0, 8)]
    [InlineData("ello-wAo", "hello-waorld", 1, 8)]
    [InlineData("ello-wZo", "hello-wzorld", 1, 8)]
    [InlineData("llo-wAor", "hello-waorld", 2, 8)]
    [InlineData("llo-wZor", "hello-wzorld", 2, 8)]
    [InlineData("lo-wAorl", "hello-waorld", 3, 8)]
    [InlineData("lo-wZorl", "hello-wzorld", 3, 8)]
    public void GetDestination_Found_IncludesCharactersWithCasingDifference(string entry, string path, int start, int length)
    {
        // Makes it easy to spot invalid tests
        Assert.Equal(entry.Length, length);
        Assert.Equal(entry, path.Substring(start, length), ignoreCase: true);

        // Arrange
        var table = CreateTable(0, -1, new[] { (entry, 1), });

        var segment = new PathSegment(start, length);

        // Act
        var result = table.GetDestination(path, segment);

        // Assert
        Assert.Equal(1, result);
    }

    // Tests for difference in casing with ASCII casing rules. Verifies our case
    // manipulation algorithm is correct.
    //
    // We convert from upper case to lower
    // '@' and '`' are 32 bits apart at the low end
    // '[' and '}' are 32 bits apart at the high end
    //
    // How to understand these tests:
    // "an @ should not be converted to a ` since it is out of range"
    [Theory]

    // character in first section non-vectorized comparisons
    [InlineData("he@", "he`lo-world", 0, 3)]
    [InlineData("he[", "he{lo-world", 0, 3)]
    [InlineData("e@", "he`lo-world", 1, 2)]
    [InlineData("e[", "he{lo-world", 1, 2)]
    [InlineData("@", "he`lo-world", 2, 1)]
    [InlineData("[", "he{lo-world", 2, 1)]

    // character in first section vectorized comparisons
    [InlineData("hel@", "hel`o-world", 0, 4)]
    [InlineData("hel[", "hel{o-world", 0, 4)]
    [InlineData("el@o", "hel`o-world", 1, 4)]
    [InlineData("el[o", "hel{o-world", 1, 4)]
    [InlineData("l@o-", "hel`o-world", 2, 4)]
    [InlineData("l[o-", "hel{o-world", 2, 4)]
    [InlineData("@o-w", "hel`o-world", 3, 4)]
    [InlineData("[o-w", "hel{o-world", 3, 4)]

    // character in second section non-vectorized comparisons
    [InlineData("hello-@", "hello-`orld", 0, 7)]
    [InlineData("hello-[", "hello-{orld", 0, 7)]
    [InlineData("ello-@", "hello-`orld", 1, 6)]
    [InlineData("ello-[", "hello-{orld", 1, 6)]
    [InlineData("llo-@", "hello-`orld", 2, 5)]
    [InlineData("llo-[", "hello-{Forld", 2, 5)]

    // character in first section vectorized comparisons
    [InlineData("hello-w@", "hello-w`orld", 0, 8)]
    [InlineData("hello-w[", "hello-w{orld", 0, 8)]
    [InlineData("ello-w@o", "hello-w`orld", 1, 8)]
    [InlineData("ello-w[o", "hello-w{orld", 1, 8)]
    [InlineData("llo-w@or", "hello-w`orld", 2, 8)]
    [InlineData("llo-w[or", "hello-w{orld", 2, 8)]
    [InlineData("lo-w@orl", "hello-w`orld", 3, 8)]
    [InlineData("lo-w[orl", "hello-w{orld", 3, 8)]
    public void GetDestination_NotFound_IncludesCharactersWithCasingDifference(string entry, string path, int start, int length)
    {
        // Makes it easy to spot invalid tests
        Assert.Equal(entry.Length, length);
        Assert.NotEqual(entry, path.Substring(start, length));

        // Arrange
        var table = CreateTable(0, -1, new[] { (entry, 1), });

        var segment = new PathSegment(start, length);

        // Act
        var result = table.GetDestination(path, segment);

        // Assert
        Assert.Equal(0, result);
    }

    // Tests for correct branching in binary search
    [Theory]
    [InlineData("hello0 world", "hello1 world", "hello2 world", "hello1 world2")]
    [InlineData("1111", "2222", "3333", "4444", "5555", "6666", "7777", "8888")] // vectorized
    [InlineData("1", "7777777", "22", "88888888", "55555", "666666", "333", "4444")]
    [InlineData("1", "4", "8", "3", "5", "2", "6", "7")] // non-vectorized
    [InlineData("1", "a", "4", "B", "8", "C", "3", "d", "5", "e", "2", "F", "6", "g", "7")] // mixed letters and numbers
    [InlineData("@", "1", "a", "4", "B", "8", "C", "3", "d", "5", "e", "2", "F", "6", "g", "7", "`")] // @ and ` are 0x20 apart which means binary search must be disabled
    public void GetDestination_Found_WithBinarySearch(params string[] segments)
    {
        // Arrange
        var entries = segments.Select((e, i) => (e, i + 1)).ToArray();
        var table = CreateTable(0, -1, entries);

        foreach (var (segment, destination) in entries)
        {
            var pathSegment = new PathSegment(0, segment.Length);

            // Act
            var result = table.GetDestination(segment, pathSegment);

            // Assert
            Assert.Equal(destination, result);
        }
    }
}
