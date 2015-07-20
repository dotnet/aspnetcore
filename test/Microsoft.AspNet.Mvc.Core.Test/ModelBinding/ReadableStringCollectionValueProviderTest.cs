// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ReadableStringCollectionValueProviderTest
    {
        private static readonly IReadableStringCollection _backingStore = new ReadableStringCollection(
            new Dictionary<string, string[]>
            {
                { "some", new[] { "someValue1", "someValue2" } },
                { "null_value", null },
                { "prefix.name", new[] { "someOtherValue" } },
                { "prefix.null_value", null },
                { "prefix.property1.property", null },
                { "prefix.property2[index]", null },
                { "prefix[index1]", null },
                { "prefix[index1].property1", null },
                { "prefix[index1].property2", null },
                { "prefix[index2].property", null },
                { "[index]", null },
                { "[index].property", null },
                { "[index][anotherIndex]", null },
            });

        [Fact]
        public async Task ContainsPrefixAsync_WithEmptyCollection_ReturnsFalseForEmptyPrefix()
        {
            // Arrange
            var backingStore = new ReadableStringCollection(new Dictionary<string, string[]>());
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_WithNonEmptyCollection_ReturnsTrueForEmptyPrefix()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync(string.Empty);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_WithNonEmptyCollection_ReturnsTrueForKnownPrefixes()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act & Assert
            Assert.True(await valueProvider.ContainsPrefixAsync("some"));
            Assert.True(await valueProvider.ContainsPrefixAsync("prefix"));
            Assert.True(await valueProvider.ContainsPrefixAsync("prefix.name"));
            Assert.True(await valueProvider.ContainsPrefixAsync("[index]"));
            Assert.True(await valueProvider.ContainsPrefixAsync("prefix[index1]"));
        }

        [Fact]
        public async Task ContainsPrefixAsync_WithNonEmptyCollection_ReturnsFalseForUnknownPrefix()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync("biff");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_EmptyPrefix_ReturnsAllPrefixes()
        {
            // Arrange
            var expected = new Dictionary<string, string>
            {
                { "index", "[index]" },
                { "null_value", "null_value" },
                { "prefix", "prefix" },
                { "some", "some" },
            };
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, culture: null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync(string.Empty);

            // Assert
            Assert.Equal(expected, result.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_UnknownPrefix_ReturnsEmptyDictionary()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync("abc");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_KnownPrefix_ReturnsMatchingItems()
        {
            // Arrange
            var expected = new Dictionary<string, string>
            {
                { "name", "prefix.name" },
                { "null_value", "prefix.null_value" },
                { "property1", "prefix.property1" },
                { "property2", "prefix.property2" },
                { "index1", "prefix[index1]" },
                { "index2", "prefix[index2]" },
            };
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync("prefix");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_IndexPrefix_ReturnsMatchingItems()
        {
            // Arrange
            var expected = new Dictionary<string, string>
            {
                { "property", "[index].property" },
                { "anotherIndex", "[index][anotherIndex]" }
            };
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync("[index]");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetValueAsync_SingleValue()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, culture);

            // Act
            var result = await valueProvider.GetValueAsync("prefix.name");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("someOtherValue", result.RawValue);
            Assert.Equal("someOtherValue", result.AttemptedValue);
            Assert.Equal(culture, result.Culture);
        }

        [Fact]
        public async Task GetValueAsync_MultiValue()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, culture);

            // Act
            var result = await valueProvider.GetValueAsync("some");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new[] { "someValue1", "someValue2" }, (IList<string>)result.RawValue);
            Assert.Equal("someValue1,someValue2", result.AttemptedValue);
            Assert.Equal(culture, result.Culture);
        }

        [Theory]
        [InlineData("null_value")]
        [InlineData("prefix.null_value")]
        public async Task GetValue_NullValue(string key)
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, culture);

            // Act
            var result = await valueProvider.GetValueAsync(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetValueAsync_NullMultipleValue()
        {
            // Arrange
            var backingStore = new ReadableStringCollection(
                new Dictionary<string, string[]>
                {
                    { "key", new string[] { null, null, "value" } }
                });
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, backingStore, culture);

            // Act
            var result = await valueProvider.GetValueAsync("key");

            // Assert
            Assert.Equal(new[] { null, null, "value" }, result.RawValue as IEnumerable<string>);
            Assert.Equal(",,value", result.AttemptedValue);
        }

        [Fact]
        public async Task GetValueAsync_ReturnsNullIfKeyNotFound()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            // Act
            var result = await valueProvider.GetValueAsync("prefix");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FilterInclude()
        {
            // Arrange
            var provider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            var bindingSource = new BindingSource(
                BindingSource.Query.Id,
                displayName: null,
                isGreedy: true,
                isFromRequest: true);

            // Act
            var result = provider.Filter(bindingSource);

            // Assert
            Assert.NotNull(result);
            Assert.Same(result, provider);
        }

        [Fact]
        public void FilterExclude()
        {
            // Arrange
            var provider = new ReadableStringCollectionValueProvider(BindingSource.Query, _backingStore, null);

            var bindingSource = new BindingSource(
                "Test",
                displayName: null,
                isGreedy: true,
                isFromRequest: true);

            // Act
            var result = provider.Filter(bindingSource);

            // Assert
            Assert.Null(result);
        }
    }
}
