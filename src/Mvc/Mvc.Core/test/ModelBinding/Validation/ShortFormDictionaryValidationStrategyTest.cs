// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ShortFormDictionaryValidationStrategyTest
{
    [Fact]
    public void EnumerateElements()
    {
        // Arrange
        var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
                { 5, "five" },
            };

        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(List<int>));
        var valueMetadata = metadataProvider.GetMetadataForType(typeof(string));
        var strategy = new ShortFormDictionaryValidationStrategy<int, string>(new Dictionary<string, int>()
            {
                { "prefix[2]", 2 },
                { "prefix[3]", 3 },
                { "prefix[5]", 5 },
            },
        valueMetadata);

        // Act
        var enumerator = strategy.GetChildren(metadata, "ignored prefix", model);

        // Assert
        Assert.Collection(
            BufferEntries(enumerator).OrderBy(e => e.Key),
            e =>
            {
                Assert.Equal("prefix[2]", e.Key);
                Assert.Equal("two", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[3]", e.Key);
                Assert.Equal("three", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[5]", e.Key);
                Assert.Equal("five", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            });
    }

    [Fact]
    public void EnumerateElements_RunOutOfIndices()
    {
        // Arrange
        var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
                { 5, "five" },
            };

        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(List<int>));
        var valueMetadata = metadataProvider.GetMetadataForType(typeof(string));
        var strategy = new ShortFormDictionaryValidationStrategy<int, string>(new Dictionary<string, int>()
            {
                { "prefix[2]", 2 },
                { "prefix[3]", 3 },
            },
        valueMetadata);

        // Act
        var enumerator = strategy.GetChildren(metadata, "ignored prefix", model);

        // Assert
        Assert.Collection(
            BufferEntries(enumerator).OrderBy(e => e.Key),
            e =>
            {
                Assert.Equal("prefix[2]", e.Key);
                Assert.Equal("two", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[3]", e.Key);
                Assert.Equal("three", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            });
    }

    [Fact]
    public void EnumerateElements_RunOutOfElements()
    {
        // Arrange
        var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
            };

        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(List<int>));
        var valueMetadata = metadataProvider.GetMetadataForType(typeof(string));
        var strategy = new ShortFormDictionaryValidationStrategy<int, string>(new Dictionary<string, int>()
            {
                { "prefix[2]", 2 },
                { "prefix[3]", 3 },
                { "prefix[5]", 5 },
            },
        valueMetadata);

        // Act
        var enumerator = strategy.GetChildren(metadata, "ignored prefix", model);

        // Assert
        Assert.Collection(
            BufferEntries(enumerator).OrderBy(e => e.Key),
            e =>
            {
                Assert.Equal("prefix[2]", e.Key);
                Assert.Equal("two", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[3]", e.Key);
                Assert.Equal("three", e.Model);
                Assert.Same(valueMetadata, e.Metadata);
            });
    }

    private List<ValidationEntry> BufferEntries(IEnumerator<ValidationEntry> enumerator)
    {
        var entries = new List<ValidationEntry>();
        while (enumerator.MoveNext())
        {
            entries.Add(enumerator.Current);
        }

        return entries;
    }
}
