// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorPagePropertyActivatorTest
    {
        [Fact]
        public void CreateViewDataDictionary_MakesNewInstance_WhenValueOnContextIsNull()
        {
            // Arrange
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                typeof(TestModel),
                new TestModelMetadataProvider(),
                propertyValueAccessors: null);
            var viewContext = new ViewContext();

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.IsType<ViewDataDictionary<TestModel>>(viewDataDictionary);
        }

        [Fact]
        public void CreateViewDataDictionary_MakesNewInstanceWithObjectModelType_WhenValueOnContextAndModelTypeAreNull()
        {
            // Arrange
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                modelType: null,
                metadataProvider: new TestModelMetadataProvider(),
                propertyValueAccessors: null);
            var viewContext = new ViewContext();

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.IsType<ViewDataDictionary<object>>(viewDataDictionary);
        }

        [Fact]
        public void CreateViewDataDictionary_CreatesNestedViewDataDictionary_WhenContextInstanceIsNonGeneric()
        {
            // Arrange
            var modelMetadataProvider = new TestModelMetadataProvider();
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                modelType: typeof(TestModel),
                metadataProvider: modelMetadataProvider,
                propertyValueAccessors: null);
            var original = new ViewDataDictionary(modelMetadataProvider, new ModelStateDictionary())
            {
                {  "test-key", "test-value" },
            };
            var viewContext = new ViewContext
            {
                ViewData = original,
            };

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.NotSame(original, viewDataDictionary);
            Assert.IsType<ViewDataDictionary<TestModel>>(viewDataDictionary);
            Assert.Equal("test-value", viewDataDictionary["test-key"]);
        }

        [Fact]
        public void CreateViewDataDictionary_CreatesNestedViewDataDictionary_WhenModelTypeDoesNotMatch()
        {
            // Arrange
            var modelMetadataProvider = new TestModelMetadataProvider();
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                modelType: typeof(TestModel),
                metadataProvider: modelMetadataProvider,
                propertyValueAccessors: null);
            var original = new ViewDataDictionary<object>(modelMetadataProvider, new ModelStateDictionary())
            {
                {  "test-key", "test-value" },
            };
            var viewContext = new ViewContext
            {
                ViewData = original,
            };

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.NotSame(original, viewDataDictionary);
            Assert.IsType<ViewDataDictionary<TestModel>>(viewDataDictionary);
            Assert.Equal("test-value", viewDataDictionary["test-key"]);
        }

        [Fact]
        public void CreateViewDataDictionary_CreatesNestedViewDataDictionary_WhenNullModelTypeDoesNotMatch()
        {
            // Arrange
            var modelMetadataProvider = new TestModelMetadataProvider();
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                modelType: null,
                metadataProvider: modelMetadataProvider,
                propertyValueAccessors: null);
            var original = new ViewDataDictionary<TestModel>(modelMetadataProvider, new ModelStateDictionary())
            {
                {  "test-key", "test-value" },
            };
            var viewContext = new ViewContext
            {
                ViewData = original,
            };

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.NotSame(original, viewDataDictionary);
            Assert.IsType<ViewDataDictionary<object>>(viewDataDictionary);
            Assert.Equal("test-value", viewDataDictionary["test-key"]);
        }

        [Fact]
        public void CreateViewDataDictionary_ReturnsInstanceOnContext_IfModelTypeMatches()
        {
            // Arrange
            var modelMetadataProvider = new TestModelMetadataProvider();
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                modelType: typeof(TestModel),
                metadataProvider: modelMetadataProvider,
                propertyValueAccessors: null);
            var original = new ViewDataDictionary<TestModel>(modelMetadataProvider, new ModelStateDictionary())
            {
                {  "test-key", "test-value" },
            };
            var viewContext = new ViewContext
            {
                ViewData = original,
            };

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.Same(original, viewDataDictionary);
        }

        [Fact]
        public void CreateViewDataDictionary_ReturnsInstanceOnContext_WithNullModelType()
        {
            // Arrange
            var modelMetadataProvider = new TestModelMetadataProvider();
            var activator = new RazorPagePropertyActivator(
                typeof(TestPage),
                modelType: null,
                metadataProvider: modelMetadataProvider,
                propertyValueAccessors: null);
            var original = new ViewDataDictionary<object>(modelMetadataProvider, new ModelStateDictionary());
            var viewContext = new ViewContext
            {
                ViewData = original,
            };

            // Act
            var viewDataDictionary = activator.CreateViewDataDictionary(viewContext);

            // Assert
            Assert.NotNull(viewDataDictionary);
            Assert.Same(original, viewDataDictionary);
        }

        private class TestPage
        {
        }

        private class TestModel
        {
        }
    }
}
