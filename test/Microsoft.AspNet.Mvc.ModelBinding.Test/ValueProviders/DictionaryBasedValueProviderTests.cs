using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProviderTestss
    {
        [Fact]
        public async Task GetValueProvider_ReturnsNull_WhenKeyIsNotFound()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "value" }
            };
            var provider = new DictionaryBasedValueProvider(values);
            
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
            var provider = new DictionaryBasedValueProvider(values);

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
            var provider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await provider.GetValueAsync("test-key");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.RawValue);
            Assert.Null(result.AttemptedValue);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsFalse_IfKeyIsNotPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider(values);

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
            var provider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await provider.ContainsPrefixAsync("test-key");

            // Assert
            Assert.True(result);
        }
    }
}
