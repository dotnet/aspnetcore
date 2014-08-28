// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelStateDictionaryTest
    {
        [Fact]
        public void CopyConstructor_CopiesModelStateData()
        {
            // Arrange
            var modelState = new ModelState
            {
                Value = GetValueProviderResult("value")
            };
            var source = new ModelStateDictionary
            {
                { "key",  modelState }
            };

            // Act
            var target = new ModelStateDictionary(source);

            // Assert
            Assert.Equal(0, target.ErrorCount);
            Assert.Equal(1, target.Count);
            Assert.Same(modelState, target["key"]);
            Assert.IsType<CopyOnWriteDictionary<string, ModelState>>(target.InnerDictionary);
        }

        [Fact]
        public void AddModelErrorCreatesModelStateIfNotPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            // Act
            dictionary.AddModelError("some key", "some error");

            // Assert
            Assert.Equal(1, dictionary.ErrorCount);
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
            Assert.Equal(2, dictionary.ErrorCount);
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
        public void GetFieldValidationState_ReturnsUnvalidatedIfDictionaryDoesNotContainKey()
        {
            // Arrange
            var msd = new ModelStateDictionary();

            // Act
            var validationState = msd.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
        }

        [Fact]
        public void GetFieldValidationState_ReturnsInvalidIfKeyChildContainsErrors()
        {
            // Arrange
            var msd = new ModelStateDictionary();
            msd.AddModelError("foo.bar", "error text");

            // Act
            var validationState = msd.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Invalid, validationState);
        }

        [Fact]
        public void GetFieldValidationState_ReturnsInvalidIfKeyContainsErrors()
        {
            // Arrange
            var msd = new ModelStateDictionary();
            msd.AddModelError("foo", "error text");

            // Act
            var validationState = msd.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Invalid, validationState);
        }

        [Fact]
        public void GetFieldValidationState_ReturnsValidIfModelStateDoesNotContainErrors()
        {
            // Arrange
            var validState = new ModelState
            {
                Value = new ValueProviderResult(null, null, null),
                ValidationState = ModelValidationState.Valid
            };
            var msd = new ModelStateDictionary
            {
                { "foo", validState }
            };

            // Act
            var validationState = msd.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Valid, validationState);
        }

        [Fact]
        public void IsValidPropertyReturnsFalseIfErrors()
        {
            // Arrange
            var errorState = new ModelState
            {
                Value = GetValueProviderResult("quux", "quux"),
                ValidationState = ModelValidationState.Invalid
            };
            var validState = new ModelState
            {
                Value = GetValueProviderResult("bar", "bar"),
                ValidationState = ModelValidationState.Valid
            };
            errorState.Errors.Add("some error");
            var dictionary = new ModelStateDictionary()
            {
                { "foo", validState },
                { "baz", errorState }
            };

            // Act
            var isValid = dictionary.IsValid;
            var validationState = dictionary.ValidationState;

            // Assert
            Assert.False(isValid);
            Assert.Equal(ModelValidationState.Invalid, validationState);
        }

        [Fact]
        public void IsValidPropertyReturnsTrueIfNoErrors()
        {
            // Arrange
            var dictionary = new ModelStateDictionary()
            {
                { "foo", new ModelState
                        {
                            ValidationState = ModelValidationState.Valid,
                            Value = GetValueProviderResult("bar", "bar")
                        }
                },
                { "baz", new ModelState
                         {
                             ValidationState = ModelValidationState.Valid,
                             Value = GetValueProviderResult("quux", "bar")
                         }
                }
            };

            // Act
            var isValid = dictionary.IsValid;
            var validationState = dictionary.ValidationState;

            // Assert
            Assert.True(isValid);
            Assert.Equal(ModelValidationState.Valid, validationState);
        }

        [Fact]
        public void IsValidPropertyReturnsFalse_IfSomeFieldsAreNotValidated()
        {
            // Arrange
            var errorState = new ModelState
            {
                Value = GetValueProviderResult("quux", "quux"),
                ValidationState = ModelValidationState.Invalid
            };
            var validState = new ModelState
            {
                Value = GetValueProviderResult("bar", "bar"),
                ValidationState = ModelValidationState.Valid
            };
            errorState.Errors.Add("some error");
            var dictionary = new ModelStateDictionary()
            {
                { "foo", validState },
                { "baz", errorState },
                { "qux", new ModelState { Value = GetValueProviderResult() }}
            };

            // Act
            var isValid = dictionary.IsValid;
            var validationState = dictionary.ValidationState;

            // Assert
            Assert.False(isValid);
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
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
            var validationState = dictionary.GetFieldValidationState("not-user");

            // Assert
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
        }

        [Fact]
        public void GetFieldValidity_ReturnsUnvalidated_IfAnyItemInSubtreeIsInvalid()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelState { ValidationState = ModelValidationState.Valid };
            dictionary.SetModelValue("user.Name", GetValueProviderResult());
            dictionary.AddModelError("user.Age", "Age is not a valid int");

            // Act
            var validationState = dictionary.GetFieldValidationState("user");

            // Assert
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
        }

        [Theory]
        [InlineData("user")]
        [InlineData("user.Age")]
        public void GetFieldValidity_ReturnsInvalid_IfAllKeysAreValidatedAndAnyEntryIsInvalid(string key)
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelState { ValidationState = ModelValidationState.Valid };
            dictionary["user.Name"] = new ModelState { ValidationState = ModelValidationState.Valid };
            dictionary.AddModelError("user.Age", "Age is not a valid int");

            // Act
            var validationState = dictionary.GetFieldValidationState(key);

            // Assert
            Assert.Equal(ModelValidationState.Invalid, validationState);
        }

        [Fact]
        public void GetFieldValidity_ReturnsValid_IfAllKeysAreValid()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelState { ValidationState = ModelValidationState.Valid };
            dictionary["user.Name"] = new ModelState { ValidationState = ModelValidationState.Valid };

            // Act
            var validationState = dictionary.GetFieldValidationState("user");

            // Assert
            Assert.Equal(ModelValidationState.Valid, validationState);
        }

        [Fact]
        public void AddModelError_WithErrorString_AddsTooManyModelErrors_WhenMaxErrorsIsReached()
        {
            // Arrange
            var expected = "The maximum number of allowed model errors has been reached.";
            var dictionary = new ModelStateDictionary
            {
                MaxAllowedErrors = 5
            };
            dictionary.AddModelError("key1", "error1");
            dictionary.AddModelError("key2", new Exception());
            dictionary.AddModelError("key3", new Exception());
            dictionary.AddModelError("key4", "error4");
            dictionary.AddModelError("key5", "error5");
            dictionary.AddModelError("key6", "error6");

            // Act and Assert
            Assert.False(dictionary.CanAddErrors);
            Assert.Equal(5, dictionary.ErrorCount);
            var error = Assert.Single(dictionary[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);
        }

        [Fact]
        public void TryAddModelError_WithErrorString_ReturnsFalse_AndAddsMaxModelErrorMessage()
        {
            // Arrange
            var expected = "The maximum number of allowed model errors has been reached.";
            var dictionary = new ModelStateDictionary
            {
                MaxAllowedErrors = 3
            };

            // Act and Assert
            var result = dictionary.TryAddModelError("key1", "error1");
            Assert.True(result);

            result = dictionary.TryAddModelError("key2", new Exception());
            Assert.True(result);

            result = dictionary.TryAddModelError("key3", "error3");
            Assert.False(result);

            result = dictionary.TryAddModelError("key4", "error4");
            Assert.False(result);

            Assert.False(dictionary.CanAddErrors);
            Assert.Equal(3, dictionary.ErrorCount);
            Assert.Equal(3, dictionary.Count);

            var error = Assert.Single(dictionary[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);
        }

        [Fact]
        public void AddModelError_WithException_AddsTooManyModelError_WhenMaxErrorIsReached()
        {
            // Arrange
            var expected = "The maximum number of allowed model errors has been reached.";
            var dictionary = new ModelStateDictionary
            {
                MaxAllowedErrors = 4
            };
            dictionary.AddModelError("key1", new Exception());
            dictionary.AddModelError("key2", "error2");
            dictionary.AddModelError("key3", "error3");
            dictionary.AddModelError("key3", new Exception());
            dictionary.AddModelError("key4", new InvalidOperationException());
            dictionary.AddModelError("key5", new FormatException());

            // Act and Assert
            Assert.False(dictionary.CanAddErrors);
            Assert.Equal(4, dictionary.ErrorCount);
            Assert.Equal(4, dictionary.Count);
            var error = Assert.Single(dictionary[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);
        }

        [Fact]
        public void TryAddModelError_WithException_ReturnsFalse_AndAddsMaxModelErrorMessage()
        {
            // Arrange
            var expected = "The maximum number of allowed model errors has been reached.";
            var dictionary = new ModelStateDictionary
            {
                MaxAllowedErrors = 3
            };

            // Act and Assert
            var result = dictionary.TryAddModelError("key1", "error1");
            Assert.True(result);

            result = dictionary.TryAddModelError("key2", new Exception());
            Assert.True(result);

            result = dictionary.TryAddModelError("key3", new Exception());
            Assert.False(result);

            Assert.Equal(3, dictionary.Count);
            var error = Assert.Single(dictionary[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);
        }

        [Fact]
        public void ModelStateDictionary_TracksAddedErrorsOverCopyConstructor()
        {
            // Arrange
            var expected = "The maximum number of allowed model errors has been reached.";
            var dictionary = new ModelStateDictionary
            {
                MaxAllowedErrors = 3
            };

            // Act
            dictionary.AddModelError("key1", "error1");
            dictionary.TryAddModelError("key3", new Exception());

            var copy = new ModelStateDictionary(dictionary);
            copy.AddModelError("key2", "error2");

            // Assert
            Assert.Equal(3, copy.Count);
            var error = Assert.Single(copy[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);
        }

        private static ValueProviderResult GetValueProviderResult(object rawValue = null, string attemptedValue = null)
        {
            return new ValueProviderResult(rawValue ?? "some value",
                                           attemptedValue ?? "some value",
                                           CultureInfo.InvariantCulture);
        }
    }
}
