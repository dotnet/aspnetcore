// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelStateDictionaryTest
{
    [Theory]
    [InlineData("")]
    [InlineData("foo")]
    public void ContainsKey_ReturnsFalse_IfNodeHasNotBeenMutated(string key)
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.AddModelError("foo.bar", "some error");

        // Act
        var result = dictionary.ContainsKey(key);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("foo")]
    public void ContainsKey_ReturnsFalse_IfNodeHasBeenRemoved(string key)
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.AddModelError(key, "some error");

        // Act
        var remove = dictionary.Remove(key);
        var containsKey = dictionary.ContainsKey(key);

        // Assert
        Assert.True(remove);
        Assert.False(containsKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("foo")]
    [InlineData("foo.bar")]
    [InlineData("foo[bar]")]
    public void ContainsKey_ReturnsTrue_IfNodeHasBeenMutated(string key)
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldSkipped(key);

        // Act
        var result = dictionary.ContainsKey(key);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("foo.bar")]
    [InlineData("foo.bar[10]")]
    public void IndexerDoesNotReturnIntermediaryNodes(string key)
    {
        // Arrange
        var modelStateDictionary = new ModelStateDictionary();
        modelStateDictionary.AddModelError("foo.bar[10].baz", "error-message");

        // Act
        var result = modelStateDictionary[key];

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("prop")]
    [InlineData("first.second")]
    [InlineData("[0].prop")]
    [InlineData("[qux]")]
    [InlineData("[test].prop")]
    [InlineData("first[0].second")]
    [InlineData("first.second[0].third")]
    [InlineData("first[second][0]")]
    [InlineData("first.second.third[0]")]
    [InlineData("first.second.third[0].fourth")]
    [InlineData("first[0][second]")]
    public void Indexer_ReturnsValuesAddedUsingSetModelValue(string key)
    {
        // Arrange
        var value = "Hello world";
        var modelStateDictionary = new ModelStateDictionary();
        modelStateDictionary.SetModelValue(key, value, value);

        // Act
        var result = modelStateDictionary[key];

        // Assert
        Assert.Equal(value, result.RawValue);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.AddModelError("a", "a-error");
        dictionary.AddModelError("b", "b-error");
        dictionary.AddModelError("c", "c-error");

        // Act
        dictionary.Clear();

        // Assert
        Assert.Empty(dictionary);
        Assert.Equal(0, dictionary.ErrorCount);
        Assert.Empty(dictionary);
        Assert.Equal(ModelValidationState.Valid, dictionary.ValidationState);
    }

    [Fact]
    public void Clear_RemovesAllEntries_IfDictionaryIsEmpty()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act
        dictionary.Clear();

        // Assert
        Assert.Empty(dictionary);
        Assert.Equal(0, dictionary.ErrorCount);
        Assert.Empty(dictionary);
        Assert.Equal(ModelValidationState.Valid, dictionary.ValidationState);
    }

    [Fact]
    public void MarkFieldSkipped_MarksFieldAsSkipped_IfStateIsUnvalidated()
    {
        // Arrange
        var source = new ModelStateDictionary();
        source.SetModelValue("key", "value", "value");

        // Act
        source.MarkFieldSkipped("key");

        // Assert
        Assert.Equal(ModelValidationState.Skipped, source["key"].ValidationState);
    }

    [Fact]
    public void MarkFieldSkipped_MarksFieldAsSkipped_IfStateIsValid()
    {
        // Arrange
        var source = new ModelStateDictionary();
        source.MarkFieldValid("key");

        // Act
        source.MarkFieldSkipped("key");

        // Assert
        Assert.Equal(ModelValidationState.Skipped, source["key"].ValidationState);
    }

    [Fact]
    public void MarkFieldSkipped_MarksFieldAsSkipped_IfKeyIsNotPresent()
    {
        // Arrange
        var source = new ModelStateDictionary();

        // Act
        source.MarkFieldSkipped("key");

        // Assert
        Assert.Equal(0, source.ErrorCount);
        Assert.Single(source);
        Assert.Equal(ModelValidationState.Skipped, source["key"].ValidationState);
    }

    [Fact]
    public void MarkFieldSkipped_Throws_IfStateIsInvalid()
    {
        // Arrange
        var source = new ModelStateDictionary();
        source.AddModelError("key", "some error");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => source.MarkFieldSkipped("key"));

        // Assert
        Assert.Equal(
            "A field previously marked invalid should not be marked skipped.",
            exception.Message);
    }

    [Fact]
    public void MarkFieldValid_MarksFieldAsValid_IfStateIsUnvalidated()
    {
        // Arrange
        var source = new ModelStateDictionary();
        source.SetModelValue("key", "value", "value");

        // Act
        source.MarkFieldValid("key");

        // Assert
        Assert.Equal(ModelValidationState.Valid, source["key"].ValidationState);
    }

    [Fact]
    public void MarkFieldValid_MarksFieldAsValid_IfStateIsSkipped()
    {
        // Arrange
        var source = new ModelStateDictionary();
        source.MarkFieldSkipped("key");

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
        Assert.Single(source);
        Assert.Equal(ModelValidationState.Valid, source["key"].ValidationState);
    }

    [Fact]
    public void MarkFieldValid_Throws_IfStateIsInvalid()
    {
        // Arrange
        var source = new ModelStateDictionary();
        source.AddModelError("key", "some-error");

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
        var source = new ModelStateDictionary();
        source.SetModelValue("key", "attempted-value", "raw-value");
        var entry = source["key"];
        entry.AttemptedValue = "attempted-value";
        entry.RawValue = "raw-value";
        entry.Errors.Add(new ModelError(new InvalidOperationException()));
        entry.Errors.Add(new ModelError("error-message"));
        entry.ValidationState = ModelValidationState.Skipped;
        // Act
        var target = new ModelStateDictionary(source);

        // Assert
        Assert.Equal(2, target.ErrorCount);
        Assert.Single(target);
        var actual = target["key"];
        Assert.Equal(entry.RawValue, actual.RawValue);
        Assert.Equal(entry.AttemptedValue, actual.AttemptedValue);
        Assert.Equal(entry.Errors, actual.Errors);
        Assert.Equal(entry.ValidationState, actual.ValidationState);
    }

    [Fact]
    public void TryAddModelException_Succeeds()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new TestException();

        // Act
        dictionary.TryAddModelException("some key", exception);

        // Assert
        var kvp = Assert.Single(dictionary);
        Assert.Equal("some key", kvp.Key);
        var error = Assert.Single(kvp.Value.Errors);
        Assert.Same(exception, error.Exception);
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
        var oldDictionary = new ModelStateDictionary();
        oldDictionary.SetModelValue("foo", "bar", "bar");

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
    [InlineData("foo[bar]")]
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
    [InlineData("foo[bar]")]
    public void GetFieldValidationState_ReturnsValidIfModelStateDoesNotContainErrors(string key)
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid(key);

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
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid(key);

        // Act
        var validationState = dictionary.GetFieldValidationState("[0].foo");

        // Assert
        Assert.Equal(ModelValidationState.Valid, validationState);
    }

    [Fact]
    public void IsValidPropertyReturnsFalseIfErrors()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("foo");
        dictionary.AddModelError("bar", "some error");

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
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("foo");
        dictionary.MarkFieldSkipped("bar");

        // Act
        var isValid = dictionary.IsValid;
        var validationState = dictionary.ValidationState;

        // Assert
        Assert.True(isValid);
        Assert.Equal(ModelValidationState.Valid, validationState);
    }

    [Fact]
    public void GetFieldValidationState_OfSkippedEntry()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.MarkFieldSkipped("foo");

        // Act
        var validationState = modelState.GetValidationState("foo");
        var fieldValidationState = modelState.GetFieldValidationState("foo");

        // Assert
        Assert.Equal(ModelValidationState.Skipped, validationState);
        Assert.Equal(ModelValidationState.Valid, fieldValidationState);
    }

    [Fact]
    public void GetFieldValidationState_WithSkippedProperty()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.MarkFieldSkipped("foo.bar.prop1");
        modelState.MarkFieldValid("foo.bar.prop2");

        // Act
        var validationState = modelState.GetFieldValidationState("foo.bar");

        // Assert
        Assert.Equal(ModelValidationState.Valid, validationState);
    }

    [Fact]
    public void GetFieldValidationState_WithAllSkippedProperties()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.MarkFieldSkipped("foo.bar.prop1");
        modelState.MarkFieldSkipped("foo.bar.prop2");

        // Act
        var validationState = modelState.GetFieldValidationState("foo.bar");

        // Assert
        Assert.Equal(ModelValidationState.Valid, validationState);
    }

    [Fact]
    public void IsValidPropertyReturnsFalse_IfSomeFieldsAreNotValidated()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("foo");
        dictionary.SetModelValue("qux", "value", "value");
        dictionary.AddModelError("baz", "some error");

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
        var dictionary1 = new ModelStateDictionary();
        dictionary1.SetModelValue("foo", "RawValue1", "AttemptedValue1");
        dictionary1.AddModelError("foo", "value1-Error1");
        dictionary1.AddModelError("foo", "value1-Error2");

        var dictionary2 = new ModelStateDictionary();
        dictionary2.SetModelValue("bar", "RawValue2", "AttemptedValue2");
        dictionary2.AddModelError("bar", "value2-Error1");

        // Act
        dictionary1.Merge(dictionary2);

        // Assert
        Assert.Equal(2, dictionary1.Count);
        var item = dictionary1["foo"];
        Assert.Equal("AttemptedValue1", item.AttemptedValue);
        Assert.Equal("RawValue1", item.RawValue);

        item = dictionary1["bar"];
        Assert.Equal("AttemptedValue2", item.AttemptedValue);
        Assert.Equal("RawValue2", item.RawValue);
        Assert.Collection(item.Errors,
            error => Assert.Equal("value2-Error1", error.ErrorMessage));
    }

    [Theory]
    [InlineData("")]
    [InlineData("key1")]
    public void MergeCopiesDictionaryOverwritesExistingValues(string key)
    {
        // Arrange
        var dictionary1 = new ModelStateDictionary();
        dictionary1.SetModelValue(key, "RawValue1", "AttemptedValue1");
        dictionary1.AddModelError(key, "value1-Error1");
        dictionary1.AddModelError(key, "value1-Error2");
        dictionary1.SetModelValue("other-key", null, null);

        var dictionary2 = new ModelStateDictionary();
        dictionary2.SetModelValue(key, "RawValue2", "AttemptedValue2");
        dictionary2.AddModelError(key, "value2-Error1");

        // Act
        dictionary1.Merge(dictionary2);

        // Assert
        Assert.Equal(2, dictionary1.Count);
        var item = dictionary1["other-key"];
        Assert.Null(item.AttemptedValue);
        Assert.Null(item.RawValue);
        Assert.Empty(item.Errors);

        item = dictionary1[key];
        Assert.Equal("AttemptedValue2", item.AttemptedValue);
        Assert.Equal("RawValue2", item.RawValue);
        Assert.Collection(item.Errors,
            error => Assert.Equal("value2-Error1", error.ErrorMessage));
    }

    [Fact]
    public void MergeDoesNothingIfParameterIsNull()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("foo", "value", "value");

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
        dictionary.MarkFieldValid("user.Address");
        dictionary.SetModelValue("user.Name", new string[] { "some value" }, "some value");
        dictionary.AddModelError("user.Age", "Age is not a valid int");

        // Act
        var validationState = dictionary.GetFieldValidationState("user");

        // Assert
        Assert.Equal(ModelValidationState.Unvalidated, validationState);
    }

    [Theory]
    [InlineData("")]
    [InlineData("user")]
    [InlineData("user.Age")]
    public void GetFieldValidity_ReturnsInvalid_IfAllKeysAreValidatedAndAnyEntryIsInvalid(string key)
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("user.Address");
        dictionary.MarkFieldValid("user.Name");
        dictionary.AddModelError("user.Age", "Age is not a valid int");

        // Act
        var validationState = dictionary.GetFieldValidationState(key);

        // Assert
        Assert.Equal(ModelValidationState.Invalid, validationState);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[0]")]
    [InlineData("[0].product")]
    public void GetFieldValidity_ReturnsInvalid_IfAllKeysAreValidatedAndAnyEntryIsInvalid_Collection(string key)
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("[0].product.Name");
        dictionary.MarkFieldValid("[0].product.Age[0]");
        dictionary.AddModelError("[0].product.Name", "Name is invalid");

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
        dictionary.MarkFieldValid("user.Address");
        dictionary.MarkFieldValid("user.Name");

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

        // Act
        dictionary.AddModelError("key1", "error1");
        dictionary.AddModelError("key2", new Exception(), metadata);
        dictionary.AddModelError("key3", new Exception(), metadata);
        dictionary.AddModelError("key4", "error4");
        dictionary.AddModelError("key5", "error5");

        // Assert
        Assert.True(dictionary.HasReachedMaxErrors);
        Assert.Equal(5, dictionary.ErrorCount);
        var error = Assert.Single(dictionary[string.Empty].Errors);
        Assert.IsType<TooManyModelErrorsException>(error.Exception);
        Assert.Equal(expected, error.Exception.Message);

        // TooManyModelErrorsException added instead of key5 error.
        Assert.DoesNotContain("key5", dictionary.Keys);
    }

    [Fact]
    public void TryAddModelException_ReturnsFalse_AndAddsMaxModelErrorMessage()
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

        result = dictionary.TryAddModelException("key2", new Exception());
        Assert.True(result);

        result = dictionary.TryAddModelException("key3", new Exception());
        Assert.False(result);

        Assert.Equal(3, dictionary.Count);
        var error = Assert.Single(dictionary[string.Empty].Errors);
        Assert.IsType<TooManyModelErrorsException>(error.Exception);
        Assert.Equal(expected, error.Exception.Message);

        // TooManyModelErrorsException added instead of key3 exception.
        Assert.DoesNotContain("key3", dictionary.Keys);
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
    public void ModelStateDictionary_ReturnExceptionMessage_WhenModelStateNotSet()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new FormatException("The supplied value is invalid for Length.");

        // Act
        dictionary.TryAddModelException("key", exception);

        // Assert
        var error = Assert.Single(dictionary["key"].Errors);
        Assert.Same(exception, error.Exception);
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
    public void ModelStateDictionary_AddsCustomErrorMessage_WhenModelStateNotSet()
    {
        // Arrange
        var expected = "Hmm, the supplied value is not valid for Length.";
        var dictionary = new ModelStateDictionary();

        var bindingMetadataProvider = CreateBindingMetadataProvider();
        var compositeProvider = new DefaultCompositeMetadataDetailsProvider(new[] { bindingMetadataProvider });
        var optionsAccessor = new OptionsAccessor();
        optionsAccessor.Value.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(
            name => $"Hmm, the supplied value is not valid for { name }.");

        var provider = new DefaultModelMetadataProvider(compositeProvider, optionsAccessor);
        var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

        // Act
        dictionary.TryAddModelError("key", new FormatException(), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_AddsCustomErrorMessage_WhenModelStateNotSet_WithParameter()
    {
        // Arrange
        var expected = "Hmm, the supplied value is not valid.";
        var dictionary = new ModelStateDictionary();

        var bindingMetadataProvider = CreateBindingMetadataProvider();
        var compositeProvider = new DefaultCompositeMetadataDetailsProvider(new[] { bindingMetadataProvider });
        var optionsAccessor = new OptionsAccessor();
        optionsAccessor.Value.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(
            () => "Hmm, the supplied value is not valid.");

        var method = typeof(string).GetMethod(nameof(string.Copy));
        var parameter = method.GetParameters()[0]; // Copy(string str)
        var provider = new DefaultModelMetadataProvider(compositeProvider, optionsAccessor);
        var metadata = provider.GetMetadataForParameter(parameter);

        // Act
        dictionary.TryAddModelError("key", new FormatException(), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_AddsCustomErrorMessage_WhenModelStateNotSet_WithType()
    {
        // Arrange
        var expected = "Hmm, the supplied value is not valid.";
        var dictionary = new ModelStateDictionary();

        var bindingMetadataProvider = CreateBindingMetadataProvider();
        var compositeProvider = new DefaultCompositeMetadataDetailsProvider(new[] { bindingMetadataProvider });
        var optionsAccessor = new OptionsAccessor();
        optionsAccessor.Value.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(
            () => "Hmm, the supplied value is not valid.");

        var provider = new DefaultModelMetadataProvider(compositeProvider, optionsAccessor);
        var metadata = provider.GetMetadataForType(typeof(int));

        // Act
        dictionary.TryAddModelError("key", new FormatException(), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public void TryAddModelException_ReturnExceptionMessage_WhenModelStateSet()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("key", new string[] { "some value" }, "some value");
        var exception = new FormatException("The value 'some value' is not valid for Length.");

        // Act
        dictionary.TryAddModelException("key", exception);

        // Assert
        var error = Assert.Single(dictionary["key"].Errors);
        Assert.Same(exception, error.Exception);
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
    public void ModelStateDictionary_AddsCustomErrorMessage_WhenModelStateSet()
    {
        // Arrange
        var expected = "Hmm, the value 'some value' is not valid for Length.";
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("key", new string[] { "some value" }, "some value");

        var bindingMetadataProvider = CreateBindingMetadataProvider();
        var compositeProvider = new DefaultCompositeMetadataDetailsProvider(new[] { bindingMetadataProvider });
        var optionsAccessor = new OptionsAccessor();
        optionsAccessor.Value.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
            (value, name) => $"Hmm, the value '{ value }' is not valid for { name }.");

        var provider = new DefaultModelMetadataProvider(compositeProvider, optionsAccessor);
        var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

        // Act
        dictionary.TryAddModelError("key", new FormatException(), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_AddsCustomErrorMessage_WhenModelStateSet_WithParameter()
    {
        // Arrange
        var expected = "Hmm, the value 'some value' is not valid.";
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("key", new string[] { "some value" }, "some value");

        var bindingMetadataProvider = CreateBindingMetadataProvider();
        var compositeProvider = new DefaultCompositeMetadataDetailsProvider(new[] { bindingMetadataProvider });
        var optionsAccessor = new OptionsAccessor();
        optionsAccessor.Value.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(
            value => $"Hmm, the value '{ value }' is not valid.");

        var method = typeof(string).GetMethod(nameof(string.Copy));
        var parameter = method.GetParameters()[0]; // Copy(string str)
        var provider = new DefaultModelMetadataProvider(compositeProvider, optionsAccessor);
        var metadata = provider.GetMetadataForParameter(parameter);

        // Act
        dictionary.TryAddModelError("key", new FormatException(), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_AddsCustomErrorMessage_WhenModelStateSet_WithType()
    {
        // Arrange
        var expected = "Hmm, the value 'some value' is not valid.";
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("key", new string[] { "some value" }, "some value");

        var bindingMetadataProvider = CreateBindingMetadataProvider();
        var compositeProvider = new DefaultCompositeMetadataDetailsProvider(new[] { bindingMetadataProvider });
        var optionsAccessor = new OptionsAccessor();
        optionsAccessor.Value.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(
            (value) => $"Hmm, the value '{ value }' is not valid.");

        var provider = new DefaultModelMetadataProvider(compositeProvider, optionsAccessor);
        var metadata = provider.GetMetadataForType(typeof(int));

        // Act
        dictionary.TryAddModelError("key", new FormatException(), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_NoErrorMessage_ForUnrecognizedException()
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
    public void TryAddModelException_AddsErrorMessage_ForInputFormatterException()
    {
        // Arrange
        var expectedMessage = "This is an InputFormatterException";
        var dictionary = new ModelStateDictionary();
        var exception = new InputFormatterException(expectedMessage);

        // Act
        dictionary.TryAddModelException("key", exception);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expectedMessage, error.ErrorMessage);
    }

    [Fact]
    public void TryAddModelException_AddsErrorMessage_ForValueProviderException()
    {
        // Arrange
        var expectedMessage = "This is an ValueProviderException";
        var dictionary = new ModelStateDictionary();
        var exception = new ValueProviderException(expectedMessage);

        // Act
        dictionary.TryAddModelException("key", exception);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expectedMessage, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_AddsErrorMessage_ForInputFormatterException()
    {
        // Arrange
        var expectedMessage = "This is an InputFormatterException";
        var dictionary = new ModelStateDictionary();

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(int));

        // Act
        dictionary.TryAddModelError("key", new InputFormatterException(expectedMessage), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expectedMessage, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_AddsErrorMessage_ForValueProviderException()
    {
        // Arrange
        var expectedMessage = "This is an ValueProviderException";
        var dictionary = new ModelStateDictionary();

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(int));

        // Act
        dictionary.TryAddModelError("key", new ValueProviderException(expectedMessage), metadata);

        // Assert
        var entry = Assert.Single(dictionary);
        Assert.Equal("key", entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(expectedMessage, error.ErrorMessage);
    }

    [Fact]
    public void ModelStateDictionary_ClearEntriesThatMatchWithKey_NonEmptyKey()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("Property1");
        dictionary.AddModelError("Property2", "Property2 invalid.");
        dictionary.AddModelError("Property3", "Property invalid.");
        dictionary.MarkFieldSkipped("Property4");

        // Act
        dictionary.ClearValidationState("Property1");
        dictionary.ClearValidationState("Property2");
        dictionary.ClearValidationState("Property4");

        // Assert
        Assert.Empty(dictionary["Property1"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property1"].ValidationState);
        Assert.Empty(dictionary["Property2"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property2"].ValidationState);
        Assert.Single(dictionary["Property3"].Errors);
        Assert.Equal(ModelValidationState.Invalid, dictionary["Property3"].ValidationState);
        Assert.Empty(dictionary["Property4"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property4"].ValidationState);
    }

    [Fact]
    public void ModelStateDictionary_ClearEntriesPrefixedWithKey_NonEmptyKey()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("Product");
        dictionary.AddModelError("Product.Detail1", "Product Detail1 invalid.");
        dictionary.AddModelError("Product.Detail2[0]", "Product Detail2[0] invalid.");
        dictionary.AddModelError("Product.Detail2[1]", "Product Detail2[1] invalid.");
        dictionary.MarkFieldSkipped("Product.Detail2[2]");
        dictionary.MarkFieldSkipped("Product.Detail3");
        dictionary.AddModelError("ProductName", "ProductName invalid.");

        // Act
        dictionary.ClearValidationState("Product");

        // Assert
        Assert.Empty(dictionary["Product"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product"].ValidationState);
        Assert.Empty(dictionary["Product.Detail1"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail1"].ValidationState);
        Assert.Empty(dictionary["Product.Detail2[0]"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail2[0]"].ValidationState);
        Assert.Empty(dictionary["Product.Detail2[1]"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail2[1]"].ValidationState);
        Assert.Empty(dictionary["Product.Detail2[2]"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail2[2]"].ValidationState);
        Assert.Empty(dictionary["Product.Detail3"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail3"].ValidationState);
        Assert.Single(dictionary["ProductName"].Errors);
        Assert.Equal(ModelValidationState.Invalid, dictionary["ProductName"].ValidationState);
    }

    [Fact]
    public void ModelStateDictionary_ClearEntries_KeyHasDot_NonEmptyKey()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("Product");
        dictionary.AddModelError("Product.Detail1", "Product Detail1 invalid.");
        dictionary.AddModelError("Product.Detail1.Name", "Product Detail1 Name invalid.");
        dictionary.MarkFieldSkipped("Product.Detail1Name");

        // Act
        dictionary.ClearValidationState("Product.Detail1");

        // Assert
        Assert.Equal(ModelValidationState.Valid, dictionary["Product"].ValidationState);
        Assert.Empty(dictionary["Product.Detail1"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Product.Detail1"].ValidationState);
        Assert.Empty(dictionary["Product.Detail1.Name"].Errors);
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
        dictionary.MarkFieldValid("Property1");
        dictionary.AddModelError("Property2", "Property2 invalid.");
        dictionary.AddModelError("Property3", "Property invalid.");
        dictionary.MarkFieldSkipped("Property4");

        // Act
        dictionary.ClearValidationState(modelKey);

        // Assert
        Assert.Empty(dictionary["Property1"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property1"].ValidationState);
        Assert.Empty(dictionary["Property2"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property2"].ValidationState);
        Assert.Empty(dictionary["Property3"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property3"].ValidationState);
        Assert.Empty(dictionary["Property4"].Errors);
        Assert.Equal(ModelValidationState.Unvalidated, dictionary["Property4"].ValidationState);
    }

    [Fact]
    public void GetEnumerable_ReturnsEmptySequenceWhenDictionaryIsEmpty()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act & Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void GetEnumerable_ReturnsAllNonContainerNodes()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("Property1");
        dictionary.SetModelValue("Property1.Property2", "value", "value");
        dictionary.AddModelError("Property2", "Property invalid.");
        dictionary.AddModelError("Property2[Property3]", "Property2[Property3] invalid.");
        dictionary.MarkFieldSkipped("Property4");
        dictionary.Remove("Property2");

        // Act & Assert
        Assert.Collection(
            dictionary,
            entry =>
            {
                Assert.Equal("Property1", entry.Key);
                Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
                Assert.Null(entry.Value.RawValue);
                Assert.Null(entry.Value.AttemptedValue);
                Assert.Empty(entry.Value.Errors);
            },
            entry =>
            {
                Assert.Equal("Property4", entry.Key);
                Assert.Equal(ModelValidationState.Skipped, entry.Value.ValidationState);
                Assert.Null(entry.Value.RawValue);
                Assert.Null(entry.Value.AttemptedValue);
                Assert.Empty(entry.Value.Errors);
            },
            entry =>
            {
                Assert.Equal("Property1.Property2", entry.Key);
                Assert.Equal(ModelValidationState.Unvalidated, entry.Value.ValidationState);
                Assert.Equal("value", entry.Value.RawValue);
                Assert.Equal("value", entry.Value.AttemptedValue);
                Assert.Empty(entry.Value.Errors);
            },
            entry =>
            {
                Assert.Equal("Property2[Property3]", entry.Key);
                Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);
                Assert.Null(entry.Value.RawValue);
                Assert.Null(entry.Value.AttemptedValue);
                Assert.Collection(entry.Value.Errors,
                    error => Assert.Equal("Property2[Property3] invalid.", error.ErrorMessage));
            });
    }

    [Fact]
    public void GetEnumerable_WorksCorrectlyWhenSiblingIsAPrefix()
    {
        // Arrange
        var modelStateDictionary = new ModelStateDictionary();
        modelStateDictionary.SetModelValue("prop", "value1", "value1");
        modelStateDictionary.SetModelValue("property_name", "value3", "value3");
        modelStateDictionary.SetModelValue("property", "value2", "value2");

        // Act & Assert
        Assert.Collection(modelStateDictionary,
            entry =>
            {
                Assert.Equal("prop", entry.Key);
                Assert.Equal("value1", entry.Value.RawValue);
            },
            entry =>
            {
                Assert.Equal("property", entry.Key);
                Assert.Equal("value2", entry.Value.RawValue);
            },
            entry =>
            {
                Assert.Equal("property_name", entry.Key);
                Assert.Equal("value3", entry.Value.RawValue);
            });
    }

    [Fact]
    public void KeysEnumerable_ReturnsEmptySequenceWhenDictionaryIsEmpty()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act
        var keys = dictionary.Keys;

        // Assert
        Assert.Empty(keys);
    }

    [Fact]
    public void KeysEnumerable_ReturnsAllKeys()
    {
        // Arrange
        var expected = new[] { "Property1", "Property4", "Property1.Property2", "Property2[Property3]" };
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("Property1");
        dictionary.AddModelError("Property1.Property2", "Property2 invalid.");
        dictionary.AddModelError("Property2", "Property invalid.");
        dictionary.AddModelError("Property2[Property3]", "Property2[Property3] invalid.");
        dictionary.MarkFieldSkipped("Property4");
        dictionary.Remove("Property2");

        // Act
        var keys = dictionary.Keys;

        // Assert
        Assert.Equal(expected, keys);
    }

    [Fact]
    public void ValuesEnumerable_ReturnsEmptySequenceWhenDictionaryIsEmpty()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act
        var values = dictionary.Values;

        // Assert
        Assert.Empty(values);
    }

    [Fact]
    public void ValuesEnumerable_ReturnsAllEntries()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MarkFieldValid("Property1");
        dictionary.SetModelValue("Property1.Property2", "value", "value");
        dictionary.AddModelError("Property2", "Property invalid.");
        dictionary.AddModelError("Property2[Property3]", "Property2[Property3] invalid.");
        dictionary.MarkFieldSkipped("Property4");
        dictionary.Remove("Property2");

        // Act & Assert
        Assert.Collection(dictionary.Values,
            value =>
            {
                Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                Assert.Null(value.RawValue);
                Assert.Null(value.AttemptedValue);
                Assert.Empty(value.Errors);
            },
            value =>
            {
                Assert.Equal(ModelValidationState.Skipped, value.ValidationState);
                Assert.Null(value.RawValue);
                Assert.Null(value.AttemptedValue);
                Assert.Empty(value.Errors);
            },
            value =>
            {
                Assert.Equal(ModelValidationState.Unvalidated, value.ValidationState);
                Assert.Equal("value", value.RawValue);
                Assert.Equal("value", value.AttemptedValue);
                Assert.Empty(value.Errors);
            },
            value =>
            {
                Assert.Equal(ModelValidationState.Invalid, value.ValidationState);
                Assert.Null(value.RawValue);
                Assert.Null(value.AttemptedValue);
                Assert.Collection(
                    value.Errors,
                    error => Assert.Equal("Property2[Property3] invalid.", error.ErrorMessage));
            });
    }

    [Fact]
    public void GetModelStateForProperty_ReturnsModelStateForImmediateChildren()
    {
        // Arrange
        var modelStateDictionary = new ModelStateDictionary();
        modelStateDictionary.SetModelValue("property1", "value1", "value1");
        modelStateDictionary.SetModelValue("property1.property2", "value2", "value2");

        // Act 1
        var property1 = modelStateDictionary.Root.GetModelStateForProperty("property1");
        var property2 = modelStateDictionary.Root.GetModelStateForProperty("property1.property2");

        // Assert 1
        Assert.Equal("value1", property1.RawValue);
        Assert.Null(property2);

        // Act 2
        property2 = property1.GetModelStateForProperty("property2");
        Assert.Equal("value2", property2.RawValue);
    }

    [Fact]
    public void GetModelStateForProperty_ReturnsModelStateForIndexedChildren()
    {
        // Arrange
        var modelStateDictionary = new ModelStateDictionary();
        modelStateDictionary.SetModelValue("[property]", "value1", "value1");

        // Act
        var property = modelStateDictionary.Root.GetModelStateForProperty("[property]");

        // Assert
        Assert.Equal("value1", property.RawValue);
    }

    [Fact]
    public void GetFieldValidationState_ReturnsUnvalidated_IfTreeHeightIsGreaterThanLimit()
    {
        // Arrange
        var stackLimit = 5;
        var dictionary = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stackLimit + 1));
        dictionary.MaxValidationDepth = stackLimit;
        dictionary.MaxStateDepth = null;
        dictionary.MarkFieldValid(key);

        // Act
        var validationState = dictionary.GetFieldValidationState("foo");

        // Assert
        Assert.Equal(ModelValidationState.Unvalidated, validationState);
    }

    [Fact]
    public void IsValidProperty_ReturnsTrue_IfTreeHeightIsGreaterThanLimit()
    {
        // Arrange
        var stackLimit = 5;
        var dictionary = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stackLimit + 1));
        dictionary.MaxValidationDepth = stackLimit;
        dictionary.MaxStateDepth = null;
        dictionary.AddModelError(key, "some error");

        // Act
        var isValid = dictionary.IsValid;
        var validationState = dictionary.ValidationState;

        // Assert
        Assert.True(isValid);
        Assert.Equal(ModelValidationState.Valid, validationState);
    }

    [Fact]
    public void TryAddModelException_Throws_IfKeyHasTooManySegments()
    {
        // Arrange
        var exception = new TestException();

        var stateDepth = 5;
        var dictionary = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stateDepth + 1));
        dictionary.MaxStateDepth = stateDepth;

        // Act
        var invalidException = Assert.Throws<InvalidOperationException>(() => dictionary.TryAddModelException(key, exception));

        // Assert
        Assert.Equal(
            $"The specified key exceeded the maximum ModelState depth: {dictionary.MaxStateDepth}",
            invalidException.Message);
    }

    [Fact]
    public void TryAddModelError_Throws_IfKeyHasTooManySegments()
    {
        // Arrange
        var stateDepth = 5;
        var dictionary = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stateDepth + 1));
        dictionary.MaxStateDepth = stateDepth;

        // Act
        var invalidException = Assert.Throws<InvalidOperationException>(() => dictionary.TryAddModelError(key, "errorMessage"));

        // Assert
        Assert.Equal(
            $"The specified key exceeded the maximum ModelState depth: {dictionary.MaxStateDepth}",
            invalidException.Message);
    }

    [Fact]
    public void SetModelValue_Throws_IfKeyHasTooManySegments()
    {
        var stateDepth = 5;
        var dictionary = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stateDepth + 1));
        dictionary.MaxStateDepth = stateDepth;

        // Act
        var invalidException = Assert.Throws<InvalidOperationException>(() => dictionary.SetModelValue(key, string.Empty, string.Empty));

        // Assert
        Assert.Equal(
            $"The specified key exceeded the maximum ModelState depth: {dictionary.MaxStateDepth}",
            invalidException.Message);
    }

    [Fact]
    public void MarkFieldValid_Throws_IfKeyHasTooManySegments()
    {
        // Arrange
        var stateDepth = 5;
        var source = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stateDepth + 1));
        source.MaxStateDepth = stateDepth;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => source.MarkFieldValid(key));

        // Assert
        Assert.Equal(
            $"The specified key exceeded the maximum ModelState depth: {source.MaxStateDepth}",
            exception.Message);
    }

    [Fact]
    public void MarkFieldSkipped_Throws_IfKeyHasTooManySegments()
    {
        // Arrange
        var stateDepth = 5;
        var source = new ModelStateDictionary();
        var key = string.Join(".", Enumerable.Repeat("foo", stateDepth + 1));
        source.MaxStateDepth = stateDepth;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => source.MarkFieldSkipped(key));

        // Assert
        Assert.Equal(
            $"The specified key exceeded the maximum ModelState depth: {source.MaxStateDepth}",
            exception.Message);
    }

    [Fact]
    public void Constructor_SetsDefaultRecursionDepth()
    {
        // Arrange && Act
        var dictionary = new ModelStateDictionary();

        // Assert
        Assert.Equal(ModelStateDictionary.DefaultMaxRecursionDepth, dictionary.MaxValidationDepth);
        Assert.Equal(ModelStateDictionary.DefaultMaxRecursionDepth, dictionary.MaxStateDepth);
    }

    [Fact]
    public void CopyConstructor_PreservesRecursionDepth()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.MaxValidationDepth = 5;
        dictionary.MaxStateDepth = 4;

        // Act
        var newDictionary = new ModelStateDictionary(dictionary);

        // Assert
        Assert.Equal(dictionary.MaxValidationDepth, newDictionary.MaxValidationDepth);
        Assert.Equal(dictionary.MaxStateDepth, newDictionary.MaxStateDepth);
    }

    private DefaultBindingMetadataProvider CreateBindingMetadataProvider()
        => new DefaultBindingMetadataProvider();

    private class OptionsAccessor : IOptions<MvcOptions>
    {
        public MvcOptions Value { get; } = new MvcOptions();
    }
}

internal class TestException : Exception
{
    public TestException()
    {
        Message = "This is a test exception";
    }

    public override string Message { get; }
}
