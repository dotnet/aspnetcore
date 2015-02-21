// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelStateDictionaryTest
    {
        [Theory]
        [InlineData(ModelValidationState.Valid)]
        [InlineData(ModelValidationState.Unvalidated)]
        public void MarkFieldSkipped_MarksFieldAsSkipped_IfStateIsNotInValid(ModelValidationState validationState)
        {
            // Arrange
            var modelState = new ModelState
            {
                Value = GetValueProviderResult("value"),
                ValidationState = validationState
            };

            var source = new ModelStateDictionary
            {
                { "key",  modelState }
            };

            // Act
            source.MarkFieldSkipped("key");

            // Assert
            Assert.Equal(ModelValidationState.Skipped, source["key"].ValidationState);
        }

        [Fact]
        public void MarkFieldSkipped_MarksFieldAsSkipped_IfKeyIsNotPresent()
        {
            // Arrange
            var modelState = new ModelState
            {
                Value = GetValueProviderResult("value"),
                ValidationState = ModelValidationState.Valid
            };

            var source = new ModelStateDictionary
            {
            };

            // Act
            source.MarkFieldSkipped("key");

            // Assert
            Assert.Equal(0, source.ErrorCount);
            Assert.Equal(1, source.Count);
            Assert.Equal(ModelValidationState.Skipped, source["key"].ValidationState);
        }

        [Fact]
        public void MarkFieldSkipped_Throws_IfStateIsInvalid()
        {
            // Arrange
            var modelState = new ModelState
            {
                Value = GetValueProviderResult("value"),
                ValidationState = ModelValidationState.Invalid
            };

            var source = new ModelStateDictionary
            {
                { "key",  modelState }
            };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => source.MarkFieldSkipped("key"));
            
            // Assert
            Assert.Equal(
                "A field previously marked invalid should not be marked skipped.",
                exception.Message);
        }

        [Theory]
        [InlineData(ModelValidationState.Skipped)]
        [InlineData(ModelValidationState.Unvalidated)]
        public void MarkFieldValid_MarksFieldAsValid_IfStateIsNotInvalid(ModelValidationState validationState)
        {
            // Arrange
            var modelState = new ModelState
            {
                Value = GetValueProviderResult("value"),
                ValidationState = validationState
            };

            var source = new ModelStateDictionary
            {
                { "key",  modelState }
            };

            // Act
            source.MarkFieldValid("key");

            // Assert
            Assert.Equal(ModelValidationState.Valid, source["key"].ValidationState);
        }

        [Fact]
        public void MarkFieldValid_MarksFieldAsValid_IfKeyIsNotPresent()
        {
            // Arrange
            var source = new ModelStateDictionary();

            // Act
            source.MarkFieldValid("key");

            // Assert
            Assert.Equal(0, source.ErrorCount);
            Assert.Equal(1, source.Count);
            Assert.Equal(ModelValidationState.Valid, source["key"].ValidationState);
        }

        [Fact]
        public void MarkFieldValid_Throws_IfStateIsInvalid()
        {
            // Arrange
            var modelState = new ModelState
            {
                Value = GetValueProviderResult("value"),
                ValidationState = ModelValidationState.Invalid
            };

            var source = new ModelStateDictionary
            {
                { "key",  modelState }
            };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => source.MarkFieldValid("key"));
            
            // Assert
            Assert.Equal(
                "A field previously marked invalid should not be marked valid.",
                exception.Message);
        }

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
        public void GetValidationState_ReturnsValidationStateForKey_IgnoresChildren()
        {
            // Arrange
            var msd = new ModelStateDictionary();
            msd.AddModelError("foo.bar", "error text");

            // Act
            var validationState = msd.GetValidationState("foo");

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
                             ValidationState = ModelValidationState.Skipped,
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

            // Act and Assert
            Assert.True(dictionary.HasReachedMaxErrors);
            Assert.Equal(5, dictionary.ErrorCount);
            var error = Assert.Single(dictionary[string.Empty].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);

            // TooManyModelErrorsException added instead of key5 error.
            Assert.DoesNotContain("key5", dictionary.Keys);
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
            Assert.False(dictionary.HasReachedMaxErrors);
            var result = dictionary.TryAddModelError("key1", "error1");
            Assert.True(result);

            Assert.False(dictionary.HasReachedMaxErrors);
            result = dictionary.TryAddModelError("key2", new Exception());
            Assert.True(result);

            Assert.False(dictionary.HasReachedMaxErrors); // Still room for TooManyModelErrorsException.
            result = dictionary.TryAddModelError("key3", "error3");
            Assert.False(result);

            Assert.True(dictionary.HasReachedMaxErrors);
            result = dictionary.TryAddModelError("key4", "error4"); // no-op
            Assert.False(result);

            Assert.True(dictionary.HasReachedMaxErrors);
            Assert.Equal(3, dictionary.ErrorCount);
            Assert.Equal(3, dictionary.Count);

            var error = Assert.Single(dictionary[string.Empty].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);

            // TooManyModelErrorsException added instead of key3 error.
            Assert.DoesNotContain("key3", dictionary.Keys);

            // Last addition did nothing.
            Assert.DoesNotContain("key4", dictionary.Keys);
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

            // Act and Assert
            Assert.True(dictionary.HasReachedMaxErrors);
            Assert.Equal(4, dictionary.ErrorCount);
            Assert.Equal(4, dictionary.Count);
            var error = Assert.Single(dictionary[string.Empty].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);

            // Second key3 model error resulted in TooManyModelErrorsException instead.
            error = Assert.Single(dictionary["key3"].Errors);
            Assert.Null(error.Exception);
            Assert.Equal("error3", error.ErrorMessage);
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
            var error = Assert.Single(dictionary[string.Empty].Errors);
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
            var error = Assert.Single(copy[string.Empty].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
            Assert.Equal(expected, error.Exception.Message);
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        public void ModelStateDictionary_HasReachedMaxErrors(int errorCount, bool expected)
        {
            // Arrange
            var dictionary = new ModelStateDictionary()
            {
                MaxAllowedErrors = 3
            };

            for (var i = 0; i < errorCount; i++)
            {
                dictionary.AddModelError("key" + i, "error");
            }

            // Act
            var canAdd = dictionary.HasReachedMaxErrors;

            // Assert
            Assert.Equal(expected, canAdd);
        }

        [Fact]
        public void ModelStateDictionary_ReturnGenericErrorMessage_WhenModelStateNotSet()
        {
            // Arrange
            var expected = "The supplied value is invalid for key.";
            var dictionary = new ModelStateDictionary();

            // Act
            dictionary.TryAddModelError("key", new FormatException());

            // Assert
            var error = Assert.Single(dictionary["key"].Errors);
            Assert.Equal(expected, error.ErrorMessage);
        }

        [Fact]
        public void ModelStateDictionary_ReturnSpecificErrorMessage_WhenModelStateSet()
        {
            // Arrange
            var expected = "The value 'some value' is not valid for key.";
            var dictionary = new ModelStateDictionary();
            dictionary.SetModelValue("key", GetValueProviderResult());

            // Act
            dictionary.TryAddModelError("key", new FormatException());

            // Assert
            var error = Assert.Single(dictionary["key"].Errors);
            Assert.Equal(expected, error.ErrorMessage);
        }

        [Fact]
        public void ModelStateDictionary_NoErrorMessage_ForNonFormatException()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.SetModelValue("key", GetValueProviderResult());

            // Act
            dictionary.TryAddModelError("key", new InvalidOperationException());

            // Assert
            var error = Assert.Single(dictionary["key"].Errors);
            Assert.Empty(error.ErrorMessage);
        }

        private static ValueProviderResult GetValueProviderResult(object rawValue = null, string attemptedValue = null)
        {
            return new ValueProviderResult(rawValue ?? "some value",
                                           attemptedValue ?? "some value",
                                           CultureInfo.InvariantCulture);
        }
    }
}
