// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelStateDictionaryExtensionsTest
{
    [Fact]
    public void AddModelError_ForSingleExpression_AddsExpectedMessage()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act
        dictionary.AddModelError<TestModel>(model => model.Text, "Message");

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Text", modelState.Key);
        Assert.Equal("Message", modelError.ErrorMessage);
    }

    [Fact]
    public void AddModelError_ForRelationExpression_AddsExpectedMessage()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act
        dictionary.AddModelError<TestModel>(model => model.Child.Text, "Message");

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Child.Text", modelState.Key);
        Assert.Equal("Message", modelError.ErrorMessage);
    }

    [Fact]
    public void AddModelError_ForImplicitlyCastedToObjectExpression_AddsExpectedMessage()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        // Act
        dictionary.AddModelError<TestModel>(model => model.Child.Value, "Message");

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Child.Value", modelState.Key);
        Assert.Equal("Message", modelError.ErrorMessage);
    }

    [Fact]
    public void AddModelError_ForNotModelsExpression_AddsExpectedMessage()
    {
        // Arrange
        var variable = "Test";
        var dictionary = new ModelStateDictionary();

        // Act
        dictionary.AddModelError<TestModel>(model => variable, "Message");

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("variable", modelState.Key);
        Assert.Equal("Message", modelError.ErrorMessage);
    }

    [Fact]
    public void TryAddModelException_ForSingleExpression_AddsExpectedException()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new Exception();

        // Act
        dictionary.TryAddModelException<TestModel>(model => model.Text, exception);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Text", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void AddModelError_ForSingleExpression_AddsExpectedException_WithModelMetadata()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new Exception();
        var provider = new TestModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(TestModel), nameof(TestModel.Text));

        // Act
        dictionary.AddModelError<TestModel>(model => model.Text, exception, metadata);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Text", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void TryAddModelException_ForRelationExpression_AddsExpectedException()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new Exception();

        // Act
        dictionary.TryAddModelException<TestModel>(model => model.Child.Text, exception);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Child.Text", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void AddModelError_ForRelationExpression_AddsExpectedException_WithModelMetadata()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new Exception();
        var provider = new TestModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(ChildModel), nameof(ChildModel.Text));

        // Act
        dictionary.AddModelError<TestModel>(model => model.Child.Text, exception, metadata);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Child.Text", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void TryAddModelException_ForImplicitlyCastedToObjectExpression_AddsExpectedException()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new Exception();

        // Act
        dictionary.TryAddModelException<TestModel>(model => model.Child.Value, exception);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Child.Value", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void AddModelError_ForImplicitlyCastedToObjectExpression_AddsExpectedException_WithModelMetadata()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var exception = new Exception();
        var provider = new TestModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(ChildModel), nameof(ChildModel.Value));

        // Act
        dictionary.AddModelError<TestModel>(model => model.Child.Value, exception, metadata);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("Child.Value", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void TryAddModelException_ForNotModelsExpression_AddsExpectedException()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var variable = "Test";
        var exception = new Exception();

        // Act
        dictionary.TryAddModelException<TestModel>(model => variable, exception);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("variable", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void AddModelError_ForNotModelsExpression_AddsExpectedException_WithModelMetadata()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        var variable = "Test";
        var exception = new Exception();
        var provider = new TestModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));

        // Act
        dictionary.AddModelError<TestModel>(model => variable, exception, metadata);

        // Assert
        var modelState = Assert.Single(dictionary);
        var modelError = Assert.Single(modelState.Value.Errors);

        Assert.Equal("variable", modelState.Key);
        Assert.Same(exception, modelError.Exception);
    }

    [Fact]
    public void Remove_ForSingleExpression_RemovesModelStateKey()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Text", "value", "value");

        // Act
        dictionary.Remove<TestModel>(model => model.Text);

        // Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void Remove_ForRelationExpression_RemovesModelStateKey()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Child.Text", "value", "value");

        // Act
        dictionary.Remove<TestModel>(model => model.Child.Text);

        // Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void Remove_ForImplicitlyCastedToObjectExpression_RemovesModelStateKey()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Child.Value", "value", "value");

        // Act
        dictionary.Remove<TestModel>(model => model.Child.Value);

        // Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void Remove_ForNotModelsExpression_RemovesModelStateKey()
    {
        // Arrange
        var variable = "Test";
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("variable", "value", "value");

        // Act
        dictionary.Remove<TestModel>(model => variable);

        // Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void RemoveAll_ForSingleExpression_RemovesModelStateKeys()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();

        dictionary.SetModelValue("Key", "value1", "value1");
        dictionary.SetModelValue("Text", "value2", "value2");
        dictionary.SetModelValue("Text.Length", "value3", "value3");
        var expected = dictionary["Key"];

        // Act
        dictionary.RemoveAll<TestModel>(model => model.Text);

        // Assert
        var modelState = Assert.Single(dictionary);

        Assert.Equal("Key", modelState.Key);
        Assert.Same(expected, modelState.Value);
    }

    [Fact]
    public void RemoveAll_ForRelationExpression_RemovesModelStateKeys()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Key", "value1", "value1");
        dictionary.SetModelValue("Child", "value2", "value2");
        dictionary.SetModelValue("Child.Text", "value3", "value3");
        var expected = dictionary["Key"];

        // Act
        dictionary.RemoveAll<TestModel>(model => model.Child);

        // Assert
        var modelState = Assert.Single(dictionary);

        Assert.Equal("Key", modelState.Key);
        Assert.Same(expected, modelState.Value);
    }

    [Fact]
    public void RemoveAll_ForImplicitlyCastedToObjectExpression_RemovesModelStateKeys()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Child", "value1", "value1");
        dictionary.SetModelValue("Child.Value", "value2", "value2");
        var expected = dictionary["child"];

        // Act
        dictionary.RemoveAll<TestModel>(model => model.Child.Value);

        // Assert
        var modelState = Assert.Single(dictionary);

        Assert.Equal("Child", modelState.Key);
        Assert.Same(expected, modelState.Value);
    }

    [Fact]
    public void RemoveAll_ForNotModelsExpression_RemovesModelStateKeys()
    {
        // Arrange
        var variable = "Test";
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Key", "value1", "value1");
        dictionary.SetModelValue("variable", "value2", "value2");
        dictionary.SetModelValue("variable.Text", "value3", "value3");
        dictionary.SetModelValue("variable.Value", "value4", "value4");

        var expected = dictionary["Key"];

        // Act
        dictionary.RemoveAll<TestModel>(model => variable);

        // Assert
        var modelState = Assert.Single(dictionary);

        Assert.Equal("Key", modelState.Key);
        Assert.Same(expected, modelState.Value);
    }

    [Fact]
    public void RemoveAll_ForModelExpression_RemovesModelPropertyKeys()
    {
        // Arrange
        var dictionary = new ModelStateDictionary();
        dictionary.SetModelValue("Key", "value1", "value1");
        dictionary.SetModelValue("Text", "value2", "value2");
        dictionary.SetModelValue("Child", "value3", "value3");
        dictionary.SetModelValue("Child.Text", "value4", "value4");
        dictionary.SetModelValue("Child.NoValue", "value5", "value5");
        var expected = dictionary["Key"];

        // Act
        dictionary.RemoveAll<TestModel>(model => model);

        // Assert
        var modelState = Assert.Single(dictionary);

        Assert.Equal("Key", modelState.Key);
        Assert.Same(expected, modelState.Value);
    }

    private class TestModel
    {
        public string Text { get; set; }

        public ChildModel Child { get; set; }
    }

    private class ChildModel
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}
