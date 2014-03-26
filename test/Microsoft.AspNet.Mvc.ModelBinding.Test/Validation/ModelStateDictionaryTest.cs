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
        public void IsValidFieldReturnsNullIfDictionaryDoesNotContainKey()
        {
            // Arrange
            var msd = new ModelStateDictionary();

            // Act
            var isValid = msd.IsValidField("foo");

            // Assert
            Assert.Null(isValid);
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
            Assert.Equal(false, isValid);
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
            Assert.Equal(false, isValid);
        }

        [Fact]
        public void IsValidFieldReturnsTrueIfModelStateDoesNotContainErrors()
        {
            // Arrange
            var msd = new ModelStateDictionary()
            {
                { "foo", new ModelState() { Value = new ValueProviderResult(null, null, null), IsValid = true } }
            };

            // Act
            var isValid = msd.IsValidField("foo");

            // Assert
            Assert.Equal(true, isValid);
        }

        [Fact]
        public void IsValidPropertyReturnsFalseIfErrors()
        {
            // Arrange
            var errorState = new ModelState() { Value = GetValueProviderResult("quux", "quux"), IsValid = false };
            errorState.Errors.Add("some error");
            var dictionary = new ModelStateDictionary()
            {
                { "foo", new ModelState() { Value = GetValueProviderResult("bar", "bar"), IsValid = true } },
                { "baz", errorState }
            };

            // Act
            var isValid = dictionary.IsValid;

            // Assert
            Assert.Equal(false, isValid);
        }

        [Fact]
        public void IsValidPropertyReturnsTrueIfNoErrors()
        {
            // Arrange
            var dictionary = new ModelStateDictionary()
            {
                { "foo", new ModelState() { IsValid = true, Value = GetValueProviderResult("bar", "bar") } },
                { "baz", new ModelState() { IsValid = true, Value = GetValueProviderResult("quux", "bar") } }
            };

            // Act
            var isValid = dictionary.IsValid;

            // Assert
            Assert.Equal(true, isValid);
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

        [Fact]
        public void GetFieldValidity_ReturnsUnvalidated_IfNoEntryExistsForKey()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.SetModelValue("user.Name", GetValueProviderResult());

            // Act
            var isValidField = dictionary.IsValidField("not-user");

            // Assert
            Assert.Equal(null, isValidField);
        }

        [Fact]
        public void GetFieldValidity_ReturnsUnvalidated_IfAnyItemInSubtreeIsInvalid()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelState { IsValid = true };
            dictionary.SetModelValue("user.Name", GetValueProviderResult());
            dictionary.AddModelError("user.Age", "Age is not a valid int");

            // Act
            var isValidField = dictionary.IsValidField("user");

            // Assert
            Assert.Equal(null, isValidField);
        }

        [Theory]
        [InlineData("user")]
        [InlineData("user.Age")]
        public void GetFieldValidity_ReturnsInvalid_IfAllKeysAreValidatedAndAnyEntryIsInvalid(string key)
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelState { IsValid = true };
            dictionary["user.Name"] = new ModelState { IsValid = true };
            dictionary.AddModelError("user.Age", "Age is not a valid int");

            // Act
            var isValidField = dictionary.IsValidField(key);

            // Assert
            Assert.Equal(false, isValidField);
        }

        [Fact]
        public void GetFieldValidity_ReturnsValid_IfAllKeysAreValid()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelState { IsValid = true };
            dictionary["user.Name"] = new ModelState { IsValid = true };

            // Act
            var isValidField = dictionary.IsValidField("user");

            // Assert
            Assert.Equal(true, isValidField);
        }

        private static ValueProviderResult GetValueProviderResult(object rawValue = null, string attemptedValue = null)
        {
            return new ValueProviderResult(rawValue ?? "some value", 
                                           attemptedValue ?? "some value", 
                                           CultureInfo.InvariantCulture);
        }
    }
}
