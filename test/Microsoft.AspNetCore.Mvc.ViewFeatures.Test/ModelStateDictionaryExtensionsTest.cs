// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
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
        public void AddModelError_ForSingleExpression_AddsExpectedException()
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
        public void AddModelError_ForRelationExpression_AddsExpectedException()
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
        public void AddModelError_ForImplicitlyCastedToObjectExpression_AddsExpectedException()
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
        public void AddModelError_ForNotModelsExpression_AddsExpectedException()
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
            dictionary.Add("Text", new ModelStateEntry());

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
            dictionary.Add("Child.Text", new ModelStateEntry());

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
            dictionary.Add("Child.Value", new ModelStateEntry());

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
            dictionary.Add("variable", new ModelStateEntry());

            // Act
            dictionary.Remove<TestModel>(model => variable);

            // Assert
            Assert.Empty(dictionary);
        }

        [Fact]
        public void RemoveAll_ForSingleExpression_RemovesModelStateKeys()
        {
            // Arrange
            var state = new ModelStateEntry();
            var dictionary = new ModelStateDictionary();

            dictionary.Add("Key", state);
            dictionary.Add("Text", new ModelStateEntry());
            dictionary.Add("Text.Length", new ModelStateEntry());

            // Act
            dictionary.RemoveAll<TestModel>(model => model.Text);

            // Assert
            var modelState = Assert.Single(dictionary);

            Assert.Equal("Key", modelState.Key);
            Assert.Same(state, modelState.Value);
        }

        [Fact]
        public void RemoveAll_ForRelationExpression_RemovesModelStateKeys()
        {
            // Arrange
            var state = new ModelStateEntry();
            var dictionary = new ModelStateDictionary();

            dictionary.Add("Key", state);
            dictionary.Add("Child", new ModelStateEntry());
            dictionary.Add("Child.Text", new ModelStateEntry());

            // Act
            dictionary.RemoveAll<TestModel>(model => model.Child);

            // Assert
            var modelState = Assert.Single(dictionary);

            Assert.Equal("Key", modelState.Key);
            Assert.Same(state, modelState.Value);
        }

        [Fact]
        public void RemoveAll_ForImplicitlyCastedToObjectExpression_RemovesModelStateKeys()
        {
            // Arrange
            var state = new ModelStateEntry();
            var dictionary = new ModelStateDictionary();

            dictionary.Add("Child", state);
            dictionary.Add("Child.Value", new ModelStateEntry());

            // Act
            dictionary.RemoveAll<TestModel>(model => model.Child.Value);

            // Assert
            var modelState = Assert.Single(dictionary);

            Assert.Equal("Child", modelState.Key);
            Assert.Same(state, modelState.Value);
        }

        [Fact]
        public void RemoveAll_ForNotModelsExpression_RemovesModelStateKeys()
        {
            // Arrange
            var variable = "Test";
            var state = new ModelStateEntry();
            var dictionary = new ModelStateDictionary();

            dictionary.Add("Key", state);
            dictionary.Add("variable", new ModelStateEntry());
            dictionary.Add("variable.Text", new ModelStateEntry());
            dictionary.Add("variable.Value", new ModelStateEntry());

            // Act
            dictionary.RemoveAll<TestModel>(model => variable);

            // Assert
            var modelState = Assert.Single(dictionary);

            Assert.Equal("Key", modelState.Key);
            Assert.Same(state, modelState.Value);
        }

        [Fact]
        public void RemoveAll_ForModelExpression_RemovesModelPropertyKeys()
        {
            // Arrange
            var state = new ModelStateEntry();
            var dictionary = new ModelStateDictionary();

            dictionary.Add("Key", state);
            dictionary.Add("Text", new ModelStateEntry());
            dictionary.Add("Child", new ModelStateEntry());
            dictionary.Add("Child.Text", new ModelStateEntry());
            dictionary.Add("Child.NoValue", new ModelStateEntry());

            // Act
            dictionary.RemoveAll<TestModel>(model => model);

            // Assert
            var modelState = Assert.Single(dictionary);

            Assert.Equal("Key", modelState.Key);
            Assert.Same(state, modelState.Value);
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
}
