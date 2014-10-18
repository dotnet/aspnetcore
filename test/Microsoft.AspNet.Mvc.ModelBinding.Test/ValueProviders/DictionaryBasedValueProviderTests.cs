// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProviderTests
    {
        [Fact]
        public async Task GetValueProvider_ReturnsNull_WhenKeyIsNotFound()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "value" }
            };
            var provider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);
            
            // Act
            var result = await provider.GetValueAsync("not-test-key");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetValueProvider_ReturnsValue_IfKeyIsPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await provider.GetValueAsync("test-key");

            // Assert
            Assert.Equal("test-value", result.RawValue);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsNullValue_IfKeyIsPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", null }
            };
            var provider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await provider.GetValueAsync("test-key");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.RawValue);
            Assert.Null(result.AttemptedValue);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("bar")]
        [InlineData("bar.baz")]
        public async Task ContainsPrefixAsync_ReturnsTrue_ForKnownPrefixes(string prefix)
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "foo", 1 },
                { "bar.baz", 1 },
            };

            var valueProvider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await valueProvider.ContainsPrefixAsync(prefix);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("bar", "1")]
        [InlineData("bar.baz", "2")]
        public async Task GetValueAsync_ReturnsCorrectValue_ForKnownKeys(string prefix, string expectedValue)
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "bar", 1 },
                { "bar.baz", 2 },
            };

            var valueProvider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await valueProvider.GetValueAsync(prefix);

            // Assert
            Assert.Equal(expectedValue, (string)result.AttemptedValue);
        }

        [Fact]
        public async Task GetValueAsync_DoesNotReturnAValue_ForAKeyPrefix()
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                { "bar.baz", 2 },
            };

            var valueProvider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await valueProvider.GetValueAsync("bar");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsFalse_IfKeyIsNotPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await provider.ContainsPrefixAsync("not-test-key");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsTrue_IfKeyIsPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = await provider.ContainsPrefixAsync("test-key");

            // Assert
            Assert.True(result);
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
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var provider = new DictionaryBasedValueProvider<TestValueProviderMetadata>(values);

            // Act
            var result = provider.Filter(metadata);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DictionaryBasedValueProvider<TestValueProviderMetadata>>(result);
        }

        private class TestValueProviderMetadata : IValueProviderMetadata
        {
        }

        private class DerivedValueProviderMetadata :TestValueProviderMetadata
        {
        }
    }
}
