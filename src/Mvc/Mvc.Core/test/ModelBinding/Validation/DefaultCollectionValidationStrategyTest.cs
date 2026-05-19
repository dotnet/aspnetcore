// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class DefaultCollectionValidationStrategyTest
{
    [Fact]
    public void EnumerateElements()
    {
        // Arrange
        var model = new List<int>() { 2, 3, 5 };

        var metadata = TestModelMetadataProvider.CreateDefaultProvider().GetMetadataForType(typeof(List<int>));
        var strategy = DefaultCollectionValidationStrategy.Instance;

        // Act
        var enumerator = strategy.GetChildren(metadata, "prefix", model);

        // Assert
        Assert.Collection(
            BufferEntries(enumerator).OrderBy(e => e.Key),
            e =>
            {
                Assert.Equal("prefix[0]", e.Key);
                Assert.Equal(2, e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[1]", e.Key);
                Assert.Equal(3, e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[2]", e.Key);
                Assert.Equal(5, e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            });
    }

    [Fact]
    public void EnumerateElements_Dictionary()
    {
        // Arrange
        var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
                { 5, "five" },
            };

        var metadata = TestModelMetadataProvider.CreateDefaultProvider().GetMetadataForType(typeof(List<int>));
        var strategy = DefaultCollectionValidationStrategy.Instance;

        // Act
        var enumerator = strategy.GetChildren(metadata, "prefix", model);

        // Assert
        Assert.Collection(
            BufferEntries(enumerator).OrderBy(e => e.Key),
            e =>
            {
                Assert.Equal("prefix[0]", e.Key);
                Assert.Equal(new KeyValuePair<int, string>(2, "two"), e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[1]", e.Key);
                Assert.Equal(new KeyValuePair<int, string>(3, "three"), e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[2]", e.Key);
                Assert.Equal(new KeyValuePair<int, string>(5, "five"), e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            });
    }

    [Fact]
    public void EnumerateElements_TwoEnumerableImplementations()
    {
        // Arrange
        var model = new TwiceEnumerable(new int[] { 2, 3, 5 });

        var metadata = TestModelMetadataProvider.CreateDefaultProvider().GetMetadataForType(typeof(TwiceEnumerable));
        var strategy = DefaultCollectionValidationStrategy.Instance;

        // Act
        var enumerator = strategy.GetChildren(metadata, "prefix", model);

        // Assert
        Assert.Collection(
            BufferEntries(enumerator).OrderBy(e => e.Key),
            e =>
            {
                Assert.Equal("prefix[0]", e.Key);
                Assert.Equal(2, e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[1]", e.Key);
                Assert.Equal(3, e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            },
            e =>
            {
                Assert.Equal("prefix[2]", e.Key);
                Assert.Equal(5, e.Model);
                Assert.Same(metadata.ElementMetadata, e.Metadata);
            });
    }

    // 'int' is chosen by validation because it's declared on the more derived type.
    private class TwiceEnumerable : List<string>, IEnumerable<int>
    {
        private readonly IEnumerable<int> _enumerable;

        public TwiceEnumerable(IEnumerable<int> enumerable)
        {
            _enumerable = enumerable;
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new InvalidOperationException();
        }
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
