// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            var entry = new ModelStateEntry
            {
                ValidationState = validationState
            };

            var source = new ModelStateDictionary
            {
                { "key",  entry }
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
            var entry = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Valid
            };

            var source = new ModelStateDictionary();

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
            var entry = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Invalid
            };

            var source = new ModelStateDictionary
            {
                { "key",  entry }
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
            var entry = new ModelStateEntry
            {
                ValidationState = validationState
            };

            var source = new ModelStateDictionary
            {
                { "key",  entry }
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
            var entry = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Invalid
            };

            var source = new ModelStateDictionary
            {
                { "key",  entry }
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
            var entry = new ModelStateEntry();
            var source = new ModelStateDictionary
            {
                { "key",  entry }
            };

            // Act
            var target = new ModelStateDictionary(source);

            // Assert
            Assert.Equal(0, target.ErrorCount);
            Assert.Equal(1, target.Count);
            Assert.Same(entry, target["key"]);
            Assert.IsType<Dictionary<string, ModelStateEntry>>(target.InnerDictionary);
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
            var exception = new Exception();
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act
            dictionary.AddModelError("some key", exception, metadata);

            // Assert
            Assert.Equal(2, dictionary.ErrorCount);
            var kvp = Assert.Single(dictionary);
            Assert.Equal("some key", kvp.Key);

            Assert.Equal(2, kvp.Value.Errors.Count);
            Assert.Equal("some error", kvp.Value.Errors[0].ErrorMessage);
            Assert.Same(exception, kvp.Value.Errors[1].Exception);
        }

        [Fact]
        public void ConstructorWithDictionaryParameter()
        {
            // Arrange
            var oldDictionary = new ModelStateDictionary()
            {
                { "foo", new ModelStateEntry() { RawValue = "bar" } }
            };

            // Act
            var newDictionary = new ModelStateDictionary(oldDictionary);

            // Assert
            Assert.Single(newDictionary);
            Assert.Equal("bar", newDictionary["foo"].RawValue);
        }

        [Fact]
        public void GetFieldValidationState_ReturnsUnvalidatedIfDictionaryDoesNotContainKey()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            // Act
            var validationState = dictionary.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
        }

        [Fact]
        public void GetValidationState_ReturnsValidationStateForKey_IgnoresChildren()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("foo.bar", "error text");

            // Act
            var validationState = dictionary.GetValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo.bar")]
        [InlineData("[0].foo.bar")]
        [InlineData("[0].foo.bar[0]")]
        public void GetFieldValidationState_ReturnsInvalidIfKeyChildContainsErrors(string key)
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError(key, "error text");

            // Act
            var validationState = dictionary.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Invalid, validationState);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo.bar")]
        [InlineData("[0].foo.bar")]
        [InlineData("[0].foo.bar[0]")]
        public void GetFieldValidationState_ReturnsValidIfModelStateDoesNotContainErrors(string key)
        {
            // Arrange
            var validState = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Valid
            };
            var dictionary = new ModelStateDictionary
            {
                { key, validState }
            };

            // Act
            var validationState = dictionary.GetFieldValidationState("foo");

            // Assert
            Assert.Equal(ModelValidationState.Valid, validationState);
        }

        [Theory]
        [InlineData("[0].foo.bar")]
        [InlineData("[0].foo.bar[0]")]
        public void GetFieldValidationState_IndexedPrefix_ReturnsInvalidIfKeyChildContainsErrors(string key)
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError(key, "error text");

            // Act
            var validationState = dictionary.GetFieldValidationState("[0].foo");

            // Assert
            Assert.Equal(ModelValidationState.Invalid, validationState);
        }

        [Theory]
        [InlineData("[0].foo.bar")]
        [InlineData("[0].foo.bar[0]")]
        public void GetFieldValidationState_IndexedPrefix_ReturnsValidIfModelStateDoesNotContainErrors(string key)
        {
            // Arrange
            var validState = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Valid
            };
            var dictionary = new ModelStateDictionary
            {
                { key, validState }
            };

            // Act
            var validationState = dictionary.GetFieldValidationState("[0].foo");

            // Assert
            Assert.Equal(ModelValidationState.Valid, validationState);
        }

        [Fact]
        public void IsValidPropertyReturnsFalseIfErrors()
        {
            // Arrange
            var errorState = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Invalid
            };
            var validState = new ModelStateEntry
            {
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
                { "foo", new ModelStateEntry
                    {
                        ValidationState = ModelValidationState.Valid,
                    }
                },
                { "baz", new ModelStateEntry
                    {
                        ValidationState = ModelValidationState.Skipped,
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
            var errorState = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Invalid
            };
            var validState = new ModelStateEntry
            {
                ValidationState = ModelValidationState.Valid
            };
            errorState.Errors.Add("some error");
            var dictionary = new ModelStateDictionary()
            {
                { "foo", validState },
                { "baz", errorState },
                { "qux", new ModelStateEntry() }
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
            var dictionary1 = new ModelStateDictionary { { "foo", new ModelStateEntry() } };
            var dictionary2 = new ModelStateDictionary { { "bar", new ModelStateEntry() } };

            // Act
            dictionary1.Merge(dictionary2);

            // Assert
            Assert.Equal(2, dictionary1.Count);
            Assert.Equal(dictionary2["bar"], dictionary1["bar"]);
        }

        [Fact]
        public void MergeDoesNothingIfParameterIsNull()
        {
            // Arrange
            var dictionary = new ModelStateDictionary() { { "foo", new ModelStateEntry() } };

            // Act
            dictionary.Merge(null);

            // Assert
            Assert.Single(dictionary);
            Assert.True(dictionary.ContainsKey("foo"));
        }

        [Fact]
        public void SetAttemptedValueCreatesModelStateIfNotPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            // Act
            dictionary.SetModelValue("some key", new string[] { "some value" }, "some value");

            // Assert
            Assert.Single(dictionary);
            var modelState = dictionary["some key"];

            Assert.Empty(modelState.Errors);
            Assert.Equal(new string[] { "some value" }, modelState.RawValue);
            Assert.Equal("some value", modelState.AttemptedValue);
        }

        [Fact]
        public void SetAttemptedValueUsesExistingModelStateIfPresent()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("some key", "some error");
            var ex = new Exception();

            // Act
            dictionary.SetModelValue("some key", new string[] { "some value" }, "some value");

            // Assert
            Assert.Single(dictionary);
            var modelState = dictionary["some key"];

            Assert.Single(modelState.Errors);
            Assert.Equal("some error", modelState.Errors[0].ErrorMessage);
            Assert.Equal(new string[] { "some value" }, modelState.RawValue);
            Assert.Equal("some value", modelState.AttemptedValue);
        }

        [Fact]
        public void GetFieldValidity_ReturnsUnvalidated_IfNoEntryExistsForKey()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.SetModelValue("user.Name", new string[] { "some value" }, "some value");

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
            dictionary["user.Address"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary.SetModelValue("user.Name", new string[] { "some value" }, "some value");
            dictionary.AddModelError("user.Age", "Age is not a valid int");

            // Act
            var validationState = dictionary.GetFieldValidationState("user");

            // Assert
            Assert.Equal(ModelValidationState.Unvalidated, validationState);
        }

        [Theory]
        [InlineData("user")]
        [InlineData("user.Age")]
        [InlineData("product")]
        public void GetFieldValidity_ReturnsInvalid_IfAllKeysAreValidatedAndAnyEntryIsInvalid(string key)
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary["user.Address"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["user.Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary.AddModelError("user.Age", "Age is not a valid int");
            dictionary["[0].product.Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["[0].product.Age[0]"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary.AddModelError("[1].product.Name", "Name is invalid");

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
            dictionary["user.Address"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["user.Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

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
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));
            dictionary.AddModelError("key1", "error1");
            dictionary.AddModelError("key2", new Exception(), metadata);
            dictionary.AddModelError("key3", new Exception(), metadata);
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
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act and Assert
            Assert.False(dictionary.HasReachedMaxErrors);
            var result = dictionary.TryAddModelError("key1", "error1");
            Assert.True(result);

            Assert.False(dictionary.HasReachedMaxErrors);
            result = dictionary.TryAddModelError("key2", new Exception(), metadata);
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
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));
            dictionary.AddModelError("key1", new Exception(), metadata);
            dictionary.AddModelError("key2", "error2");
            dictionary.AddModelError("key3", "error3");
            dictionary.AddModelError("key3", new Exception(), metadata);

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
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act and Assert
            var result = dictionary.TryAddModelError("key1", "error1");
            Assert.True(result);

            result = dictionary.TryAddModelError("key2", new Exception(), metadata);
            Assert.True(result);

            result = dictionary.TryAddModelError("key3", new Exception(), metadata);
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
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act
            dictionary.AddModelError("key1", "error1");
            dictionary.TryAddModelError("key3", new Exception(), metadata);

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
            var expected = "The supplied value is invalid for Length.";
            var dictionary = new ModelStateDictionary();
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act
            dictionary.TryAddModelError("key", new FormatException(), metadata);

            // Assert
            var error = Assert.Single(dictionary["key"].Errors);
            Assert.Equal(expected, error.ErrorMessage);
        }

        [Fact]
        public void ModelStateDictionary_ReturnSpecificErrorMessage_WhenModelStateSet()
        {
            // Arrange
            var expected = "The value 'some value' is not valid for Length.";
            var dictionary = new ModelStateDictionary();
            dictionary.SetModelValue("key", new string[] { "some value" }, "some value");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act
            dictionary.TryAddModelError("key", new FormatException(), metadata);

            // Assert
            var error = Assert.Single(dictionary["key"].Errors);
            Assert.Equal(expected, error.ErrorMessage);
        }

        [Fact]
        public void ModelStateDictionary_NoErrorMessage_ForNonFormatException()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();
            dictionary.SetModelValue("key", new string[] { "some value" }, "some value");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

            // Act
            dictionary.TryAddModelError("key", new InvalidOperationException(), metadata);

            // Assert
            var error = Assert.Single(dictionary["key"].Errors);
            Assert.Empty(error.ErrorMessage);
        }

        [Fact]
        public void ModelStateDictionary_ClearEntriesThatMatchWithKey_NonEmptyKey()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            dictionary["Property1"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

            dictionary["Property2"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Property2", "Property2 invalid.");

            dictionary["Property3"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Property3", "Property invalid.");

            dictionary["Property4"] = new ModelStateEntry { ValidationState = ModelValidationState.Skipped };

            // Act
            dictionary.ClearValidationState("Property1");
            dictionary.ClearValidationState("Property2");
            dictionary.ClearValidationState("Property4");

            // Assert
            Assert.Equal(0, dictionary["Property1"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property1"].ValidationState);
            Assert.Equal(0, dictionary["Property2"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property2"].ValidationState);
            Assert.Equal(1, dictionary["Property3"].Errors.Count);
            Assert.Equal(ModelValidationState.Invalid, dictionary["Property3"].ValidationState);
            Assert.Equal(0, dictionary["Property4"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property4"].ValidationState);
        }

        [Fact]
        public void ModelStateDictionary_ClearEntriesPrefixedWithKey_NonEmptyKey()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            dictionary["Product"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

            dictionary["Product.Detail1"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Product.Detail1", "Product Detail1 invalid.");

            dictionary["Product.Detail2[0]"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Product.Detail2[0]", "Product Detail2[0] invalid.");

            dictionary["Product.Detail2[1]"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Product.Detail2[1]", "Product Detail2[1] invalid.");

            dictionary["Product.Detail2[2]"] = new ModelStateEntry { ValidationState = ModelValidationState.Skipped };

            dictionary["Product.Detail3"] = new ModelStateEntry { ValidationState = ModelValidationState.Skipped };

            dictionary["ProductName"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("ProductName", "ProductName invalid.");

            // Act
            dictionary.ClearValidationState("Product");

            // Assert
            Assert.Equal(0, dictionary["Product"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail1"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail1"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail2[0]"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail2[0]"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail2[1]"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail2[1]"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail2[2]"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail2[2]"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail3"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail3"].ValidationState);
            Assert.Equal(1, dictionary["ProductName"].Errors.Count);
            Assert.Equal(ModelValidationState.Invalid, dictionary["ProductName"].ValidationState);
        }

        [Fact]
        public void ModelStateDictionary_ClearEntries_KeyHasDot_NonEmptyKey()
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            dictionary["Product"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

            dictionary["Product.Detail1"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Product.Detail1", "Product Detail1 invalid.");

            dictionary["Product.Detail1.Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Product.Detail1.Name", "Product Detail1 Name invalid.");

            dictionary["Product.Detail1Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Skipped };

            // Act
            dictionary.ClearValidationState("Product.Detail1");

            // Assert
            Assert.Equal(ModelValidationState.Valid, dictionary["Product"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail1"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail1"].ValidationState);
            Assert.Equal(0, dictionary["Product.Detail1.Name"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail1.Name"].ValidationState);
            Assert.Equal(ModelValidationState.Skipped, dictionary["Product.Detail1Name"].ValidationState);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ModelStateDictionary_ClearsAllEntries_EmptyKey(string modelKey)
        {
            // Arrange
            var dictionary = new ModelStateDictionary();

            dictionary["Property1"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

            dictionary["Property2"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Property2", "Property2 invalid.");

            dictionary["Property3"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Property3", "Property invalid.");

            dictionary["Property4"] = new ModelStateEntry { ValidationState = ModelValidationState.Skipped };

            // Act
            dictionary.ClearValidationState(modelKey);

            // Assert
            Assert.Equal(0, dictionary["Property1"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property1"].ValidationState);
            Assert.Equal(0, dictionary["Property2"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property2"].ValidationState);
            Assert.Equal(0, dictionary["Property3"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property3"].ValidationState);
            Assert.Equal(0, dictionary["Property4"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property4"].ValidationState);
        }
    }
}