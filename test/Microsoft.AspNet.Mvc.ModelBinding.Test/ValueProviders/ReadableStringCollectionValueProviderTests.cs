// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities.Collections;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ReadableStringCollectionValueProviderTest
    {
        private static readonly IReadableStringCollection _backingStore = new ReadableStringCollection(
            new Dictionary<string, string[]>
            {
                { "foo", new[] { "fooValue1", "fooValue2"} },
                { "bar.baz", new[] {"someOtherValue" } },
                { "null_value", null },
                { "prefix.null_value", null }
            });

        [Fact]
        public async Task ContainsPrefixAsync_WithEmptyCollection_ReturnsFalseForEmptyPrefix()
        {
            // Arrange
            var backingStore = new ReadableStringCollection(new Dictionary<string, string[]>());
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_WithNonEmptyCollection_ReturnsTrueForEmptyPrefix()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync("");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_WithNonEmptyCollection_ReturnsTrueForKnownPrefixes()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

            // Act & Assert
            Assert.True(await valueProvider.ContainsPrefixAsync("foo"));
            Assert.True(await valueProvider.ContainsPrefixAsync("bar"));
            Assert.True(await valueProvider.ContainsPrefixAsync("bar.baz"));
        }

        [Fact]
        public async Task ContainsPrefixAsync_WithNonEmptyCollection_ReturnsFalseForUnknownPrefix()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

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
                { "bar", "bar" },
                { "foo", "foo" },
                { "null_value", "null_value" },
                { "prefix", "prefix" }
            };
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, culture: null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync("");

            // Assert
            Assert.Equal(expected, result.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_UnknownPrefix_ReturnsEmptyDictionary()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync("abc");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_KnownPrefix_ReturnsMatchingItems()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

            // Act
            var result = await valueProvider.GetKeysFromPrefixAsync("bar");

            // Assert
            var kvp = Assert.Single(result);
            Assert.Equal("baz", kvp.Key);
            Assert.Equal("bar.baz", kvp.Value);
        }

        [Fact]
        public async Task GetValueAsync_SingleValue()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("bar.baz");

            // Assert
            Assert.NotNull(vpResult);
            Assert.Equal("someOtherValue", vpResult.RawValue);
            Assert.Equal("someOtherValue", vpResult.AttemptedValue);
            Assert.Equal(culture, vpResult.Culture);
        }

        [Fact]
        public async Task GetValueAsync_MultiValue()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("foo");

            // Assert
            Assert.NotNull(vpResult);
            Assert.Equal(new[] { "fooValue1", "fooValue2" }, (IList<string>)vpResult.RawValue);
            Assert.Equal("fooValue1,fooValue2", vpResult.AttemptedValue);
            Assert.Equal(culture, vpResult.Culture);
        }

        // TODO: Determine if this is still relevant. Right now the lookup returns null while
        // we expect a ValueProviderResult that wraps a null value.
        //[Theory]
        //[InlineData("null_value")]
        //[InlineData("prefix.null_value")]
        //public async Task GetValue_NullValue(string key)
        //{
        //    // Arrange
        //    var culture = new CultureInfo("fr-FR");
        //    var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, culture);

        //    // Act
        //    ValueProviderResult vpResult = valueProvider.GetValue(key);

        //    // Assert
        //    Assert.NotNull(vpResult);
        //    Assert.Equal(null, vpResult.RawValue);
        //    Assert.Equal(null, vpResult.AttemptedValue);
        //    Assert.Equal(culture, vpResult.Culture);
        //}

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
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(backingStore, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("key");

            // Assert
            Assert.Equal(new[] { null, null, "value" }, vpResult.RawValue as IEnumerable<string>);
            Assert.Equal(",,value", vpResult.AttemptedValue);
        }

        [Fact]
        public async Task GetValueAsync_ReturnsNullIfKeyNotFound()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

            // Act
            var vpResult = await valueProvider.GetValueAsync("bar");

            // Assert
            Assert.Null(vpResult);
        }

        public static IEnumerable<object[]> RegisteredAsMetadataClasses
        {
            get
            {
                yield return new object[] { new TestValueProviderMetadata() };
                yield return new object[] { new DerivedValueProviderMetadata() };
            }
        }

        [Theory]
        [MemberData(nameof(RegisteredAsMetadataClasses))]
        public void FilterReturnsItself_ForAnyClassRegisteredAsGenericParam(IValueProviderMetadata metadata)
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider<TestValueProviderMetadata>(_backingStore, null);

            // Act
            var result = valueProvider.Filter(metadata);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ReadableStringCollectionValueProvider<TestValueProviderMetadata>>(result);
        }

        private class TestValueProviderMetadata : IValueProviderMetadata
        {
        }

        private class DerivedValueProviderMetadata : TestValueProviderMetadata
        {
        }
    }
}
