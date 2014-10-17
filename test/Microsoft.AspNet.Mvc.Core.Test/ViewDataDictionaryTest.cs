// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ViewDataDictionaryTest
    {
        [Fact]
        public void ConstructorWithOneParameterInitalizesMembers()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();

            // Act
            var viewData = new ViewDataDictionary(metadataProvider);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.Null(viewData.ModelMetadata);
            Assert.Equal(0, viewData.Count);
        }

        [Fact]
        public void ConstructorInitalizesMembers()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelState = new ModelStateDictionary();

            // Act
            var viewData = new ViewDataDictionary(metadataProvider, modelState);

            // Assert
            Assert.Same(modelState, viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.Null(viewData.ModelMetadata);
            Assert.Equal(0, viewData.Count);
        }

        [Fact]
        public void SetModelUsesPassedInModelMetadataProvider()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider.Setup(m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(TestModel)))
                            .Returns(new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(TestModel)))
                            .Verifiable();
            var modelState = new ModelStateDictionary();
            var viewData = new TestViewDataDictionary(metadataProvider.Object, modelState);
            var model = new TestModel();

            // Act
            viewData.SetModelPublic(model);

            // Assert
            Assert.NotNull(viewData.ModelMetadata);
            metadataProvider.Verify();
        }

        [Fact]
        public void CopyConstructorInitalizesModelAndModelMetadataBasedOnSource()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = model
            };
            source["foo"] = "bar";

            // Act
            var viewData = new ViewDataDictionary(source);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
            Assert.Same(model, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
            Assert.Equal("bar", viewData["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorUsesPassedInModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = "string model"
            };
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary(source, model);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Same(model, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorDoesNotThrowOnNullModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary(metadataProvider);
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary(source, null);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.Null(viewData.ModelMetadata);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorDoesNotThrowOnNullModel_WithValueTypeTModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary(metadataProvider);
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary<int>(source, null);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Throws<NullReferenceException>(() => viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        private class TestModel
        {
        }

        private class TestViewDataDictionary : ViewDataDictionary
        {
            public TestViewDataDictionary(IModelMetadataProvider modelMetadataProvider,
                                          ModelStateDictionary modelState)
                : base(modelMetadataProvider, modelState)
            {
            }

            public TestViewDataDictionary(ViewDataDictionary source)
                : base(source)
            {
            }

            public void SetModelPublic(object value)
            {
                SetModel(value);
            }
        }
    }
}