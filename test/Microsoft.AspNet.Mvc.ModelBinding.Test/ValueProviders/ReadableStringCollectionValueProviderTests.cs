using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.PipelineCore.Collections;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ReadableStringCollectionValueProviderTest
    {
        private static readonly IReadableStringCollection _backingStore = new ReadableStringCollection(
            new Dictionary<string, string[]>
            {
                {"foo", new[] { "fooValue1", "fooValue2"} },
                {"bar.baz", new[] {"someOtherValue" }},
                {"null_value", null},
                {"prefix.null_value", null}
            });


        [Fact]
        public async Task ContainsPrefix_WithEmptyCollection_ReturnsFalseForEmptyPrefix()
        {
            // Arrange
            var backingStore = new ReadableStringCollection(new Dictionary<string, string[]>());
            var valueProvider = new ReadableStringCollectionValueProvider(backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ContainsPrefix_WithNonEmptyCollection_ReturnsTrueForEmptyPrefix()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync("");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ContainsPrefix_WithNonEmptyCollection_ReturnsTrueForKnownPrefixes()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act & Assert
            Assert.True(await valueProvider.ContainsPrefixAsync("foo"));
            Assert.True(await valueProvider.ContainsPrefixAsync("bar"));
            Assert.True(await valueProvider.ContainsPrefixAsync("bar.baz"));
        }

        [Fact]
        public async Task ContainsPrefix_WithNonEmptyCollection_ReturnsFalseForUnknownPrefix()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act
            var result = await valueProvider.ContainsPrefixAsync("biff");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetKeysFromPrefix_EmptyPrefix_ReturnsAllPrefixes()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act
            IDictionary<string, string> result = valueProvider.GetKeysFromPrefix("");

            // Assert
            Assert.Equal<KeyValuePair<string, string>>(
                result.OrderBy(kvp => kvp.Key),
                new Dictionary<string, string> { { "bar", "bar" }, { "foo", "foo" }, { "null_value", "null_value" }, { "prefix", "prefix" } });
        }

        [Fact]
        public void GetKeysFromPrefix_UnknownPrefix_ReturnsEmptyDictionary()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act
            IDictionary<string, string> result = valueProvider.GetKeysFromPrefix("abc");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetKeysFromPrefix_KnownPrefix_ReturnsMatchingItems()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act
            IDictionary<string, string> result = valueProvider.GetKeysFromPrefix("bar");

            // Assert
            KeyValuePair<string, string> kvp = Assert.Single(result);
            Assert.Equal("baz", kvp.Key);
            Assert.Equal("bar.baz", kvp.Value);
        }

        [Fact]
        public async Task GetValue_SingleValue()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("bar.baz");

            // Assert
            Assert.NotNull(vpResult);
            Assert.Equal("someOtherValue", vpResult.RawValue);
            Assert.Equal("someOtherValue", vpResult.AttemptedValue);
            Assert.Equal(culture, vpResult.Culture);
        }

        [Fact]
        public async Task GetValue_MultiValue()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("foo");

            // Assert
            Assert.NotNull(vpResult);
            Assert.Equal(new [] { "fooValue1", "fooValue2" }, (IList<string>)vpResult.RawValue);
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
        public async Task GetValue_NullMultipleValue()
        {
            // Arrange
            var backingStore = new ReadableStringCollection(
                new Dictionary<string, string[]>
                { 
                    { "key", new string[] { null, null, "value" } }
                });
            var culture = new CultureInfo("fr-FR");
            var valueProvider = new ReadableStringCollectionValueProvider(backingStore, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("key");

            // Assert
            Assert.Equal(new[] { null, null, "value" }, vpResult.RawValue as IEnumerable<string>);
            Assert.Equal(",,value", vpResult.AttemptedValue);
        }

        [Fact]
        public async Task GetValue_ReturnsNullIfKeyNotFound()
        {
            // Arrange
            var valueProvider = new ReadableStringCollectionValueProvider(_backingStore, null);

            // Act
            var vpResult = await valueProvider.GetValueAsync("bar");

            // Assert
            Assert.Null(vpResult);
        }
    }
}
