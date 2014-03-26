using System;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelStateDictionaryTest
    {
        [Fact]
        public void AddModelErrorCreatesModelStateIfNotPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            // Act
            dictionary.AddModelError("some key", "some error");

            // Assert
            var kvp = Assert.Single(dictionary);
            Assert.Equal("some key", kvp.Key);
            var error = Assert.Single(kvp.Value.Errors);
            Assert.Equal("some error", error.ErrorMessage);
        }

        [Fact]
        public void AddModelErrorUsesExistingModelStateIfPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("some key", "some error");
            var ex = new Exception();

            // Act
            dictionary.AddModelError("some key", ex);

            // Assert
            var kvp = Assert.Single(dictionary);
            Assert.Equal("some key", kvp.Key);

            Assert.Equal(2, kvp.Value.Errors.Count);
            Assert.Equal("some error", kvp.Value.Errors[0].ErrorMessage);
            Assert.Same(ex, kvp.Value.Errors[1].Exception);
        }

        [Fact]
        public void ConstructorWithDictionaryParameter()
        {
            // Arrange
            var oldDictionary = new ModelStateDictionary()
            {
                { "foo", new ModelState() { Value = GetValueProviderResult("bar", "bar") } }
            };

            // Act
            var newDictionary = new ModelStateDictionary(oldDictionary);

            // Assert
            Assert.Single(newDictionary);
            Assert.Equal("bar", newDictionary["foo"].Value.ConvertTo(typeof(string)));
        }

        [Fact]
        public void IsValidFieldReturnsFalseIfDictionaryDoesNotContainKey()
        {
            // Arrange
            var msd = new ModelStateDictionary();

            // Act
            var isValid = msd.IsValidField("foo");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidFieldReturnsFalseIfKeyChildContainsErrors()
        {
            // Arrange
            var msd = new ModelStateDictionary();
            msd.AddModelError("foo.bar", "error text");

            // Act
            var isValid = msd.IsValidField("foo");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFieldReturnsFalseIfKeyContainsErrors()
        {
            // Arrange
            var msd = new ModelStateDictionary();
            msd.AddModelError("foo", "error text");

            // Act
            var isValid = msd.IsValidField("foo");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFieldReturnsTrueIfModelStateDoesNotContainErrors()
        {
            // Arrange
            var msd = new ModelStateDictionary()
            {
                { "foo", new ModelState() { Value = new ValueProviderResult(null, null, null) } }
            };

            // Act
            var isValid = msd.IsValidField("foo");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidPropertyReturnsFalseIfErrors()
        {
            // Arrange
            var errorState = new ModelState() { Value = GetValueProviderResult("quux", "quux") };
            errorState.Errors.Add("some error");
            var dictionary = new ModelStateDictionary()
            {
                { "foo", new ModelState() { Value = GetValueProviderResult("bar", "bar") } },
                { "baz", errorState }
            };

            // Act
            var isValid = dictionary.IsValid;

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidPropertyReturnsTrueIfNoErrors()
        {
            // Arrange
            var dictionary = new ModelStateDictionary()
            {
                { "foo", new ModelState() { Value = GetValueProviderResult("bar", "bar") } },
                { "baz", new ModelState() { Value = GetValueProviderResult("quux", "bar") } }
            };

            // Act
            var isValid = dictionary.IsValid;

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void MergeCopiesDictionaryEntries()
        {
            // Arrange
            var fooDict = new ModelStateDictionary() { { "foo", new ModelState() } };
            var barDict = new ModelStateDictionary() { { "bar", new ModelState() } };

            // Act
            fooDict.Merge(barDict);

            // Assert
            Assert.Equal(2, fooDict.Count);
            Assert.Equal(barDict["bar"], fooDict["bar"]);
        }

        [Fact]
        public void MergeDoesNothingIfParameterIsNull()
        {
            // Arrange
            var fooDict = new ModelStateDictionary() { { "foo", new ModelState() } };

            // Act
            fooDict.Merge(null);

            // Assert
            Assert.Single(fooDict);
            Assert.True(fooDict.ContainsKey("foo"));
        }

        [Fact]
        public void SetAttemptedValueCreatesModelStateIfNotPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            // Act
            dictionary.SetModelValue("some key", GetValueProviderResult("some value", "some value"));

            // Assert
            Assert.Single(dictionary);
            var modelState = dictionary["some key"];

            Assert.Empty(modelState.Errors);
            Assert.Equal("some value", modelState.Value.ConvertTo(typeof(string)));
        }

        [Fact]
        public void SetAttemptedValueUsesExistingModelStateIfPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("some key", "some error");
            var ex = new Exception();

            // Act
            dictionary.SetModelValue("some key", GetValueProviderResult("some value", "some value"));

            // Assert
            Assert.Single(dictionary);
            var modelState = dictionary["some key"];

            Assert.Single(modelState.Errors);
            Assert.Equal("some error", modelState.Errors[0].ErrorMessage);
            Assert.Equal("some value", modelState.Value.ConvertTo(typeof(string)));
        }

        private static ValueProviderResult GetValueProviderResult(object rawValue, string attemptedValue)
        {
            return new ValueProviderResult(rawValue, attemptedValue, CultureInfo.InvariantCulture);
        }
    }
}
