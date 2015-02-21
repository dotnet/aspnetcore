// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ViewDataDictionaryOfTModelTest
    {
        [Fact]
        public void Constructor_InitalizesMembers()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelState = new ModelStateDictionary();

            // Act
            var viewData = new ViewDataDictionary<string>(metadataProvider, modelState);

            // Assert
            Assert.Same(modelState, viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(0, viewData.Count);
        }

        [Fact]
        public void CopyConstructors_InitalizeModelAndModelMetadataBasedOnSource()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary<object>(metadataProvider)
            {
                Model = model
            };
            source["foo"] = "bar";
            source.TemplateInfo.HtmlFieldPrefix = "prefix";

            // Act
            var viewData1 = new ViewDataDictionary<object>(source);
            var viewData2 = new ViewDataDictionary(source);

            // Assert
            Assert.NotNull(viewData1.ModelState);
            Assert.NotNull(viewData1.TemplateInfo);
            Assert.Equal("prefix", viewData1.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData1.TemplateInfo);
            Assert.Same(model, viewData1.Model);
            Assert.NotNull(viewData1.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData1.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData1.ModelMetadata);
            Assert.Equal(source.Count, viewData1.Count);
            Assert.Equal("bar", viewData1["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData1.Data);

            Assert.NotNull(viewData2.ModelState);
            Assert.NotNull(viewData2.TemplateInfo);
            Assert.Equal("prefix", viewData2.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData2.TemplateInfo);
            Assert.Same(model, viewData2.Model);
            Assert.NotNull(viewData2.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData2.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData2.ModelMetadata);
            Assert.Equal(source.Count, viewData2.Count);
            Assert.Equal("bar", viewData2["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData2.Data);
        }

        [Fact]
        public void CopyConstructors_InitalizeModelAndModelMetadataBasedOnSource_NullModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary<TestModel>(metadataProvider);
            source["foo"] = "bar";
            source.TemplateInfo.HtmlFieldPrefix = "prefix";

            // Act
            var viewData1 = new ViewDataDictionary<TestModel>(source);
            var viewData2 = new ViewDataDictionary(source);

            // Assert
            Assert.NotNull(viewData1.ModelState);
            Assert.NotNull(viewData1.TemplateInfo);
            Assert.Equal("prefix", viewData1.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData1.TemplateInfo);
            Assert.Null(viewData1.Model);
            Assert.NotNull(viewData1.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData1.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData1.ModelMetadata);
            Assert.Equal(source.Count, viewData1.Count);
            Assert.Equal("bar", viewData1["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData1.Data);

            Assert.NotNull(viewData2.ModelState);
            Assert.NotNull(viewData2.TemplateInfo);
            Assert.Equal("prefix", viewData2.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData2.TemplateInfo);
            Assert.Null(viewData2.Model);
            Assert.NotNull(viewData2.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData2.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData2.ModelMetadata);
            Assert.Equal(source.Count, viewData2.Count);
            Assert.Equal("bar", viewData2["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData2.Data);
        }

        [Fact]
        public void CopyConstructors_InitalizeModelAndModelMetadataBasedOnSource_ModelOfSubclass()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new SupremeTestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = model,
            };
            source["foo"] = "bar";
            source.TemplateInfo.HtmlFieldPrefix = "prefix";

            // Act
            var viewData1 = new ViewDataDictionary<TestModel>(source);
            var viewData2 = new ViewDataDictionary(source);

            // Assert
            Assert.NotNull(viewData1.ModelState);
            Assert.NotNull(viewData1.TemplateInfo);
            Assert.Equal("prefix", viewData1.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData1.TemplateInfo);
            Assert.Same(model, viewData1.Model);
            Assert.NotNull(viewData1.ModelMetadata);
            Assert.Equal(typeof(SupremeTestModel), viewData1.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData1.ModelMetadata);
            Assert.Equal(source.Count, viewData1.Count);
            Assert.Equal("bar", viewData1["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData1.Data);

            Assert.NotNull(viewData2.ModelState);
            Assert.NotNull(viewData2.TemplateInfo);
            Assert.Equal("prefix", viewData2.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData2.TemplateInfo);
            Assert.Same(model, viewData2.Model);
            Assert.NotNull(viewData2.ModelMetadata);
            Assert.Equal(typeof(SupremeTestModel), viewData2.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData2.ModelMetadata);
            Assert.Equal(source.Count, viewData2.Count);
            Assert.Equal("bar", viewData2["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData2.Data);
        }

        [Fact]
        public void CopyConstructor_DoesNotThrowOnNullModel_WithValueTypeTModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary(metadataProvider);
            source["key1"] = "value1";
            source.TemplateInfo.HtmlFieldPrefix = "prefix";

            // Act
            var viewData = new ViewDataDictionary<int>(source, model: null);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Equal("prefix", viewData.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
            Assert.Equal(0, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(int), viewData.ModelMetadata.ModelType);
            Assert.NotSame(source.ModelMetadata, viewData.ModelMetadata);
            Assert.Equal(source.Count, viewData.Count);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructors_OverrideSourceMetadata_IfDeclaredTypeChanged()
        {
            // Arrange
            var expectedType = typeof(string);
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary<int>(metadataProvider);

            // Act
            var viewData1 = new ViewDataDictionary<string>(source);
            var viewData2 = new ViewDataDictionary<string>(source, model: null);

            // Assert
            Assert.NotNull(viewData1.ModelMetadata);
            Assert.Equal(expectedType, viewData1.ModelMetadata.ModelType);
            Assert.Equal(expectedType, viewData1.ModelMetadata.RealModelType);

            Assert.NotNull(viewData2.ModelMetadata);
            Assert.Equal(expectedType, viewData2.ModelMetadata.ModelType);
            Assert.Equal(expectedType, viewData2.ModelMetadata.RealModelType);
        }

        [Fact]
        public void CopyConstructors_ThrowInvalidOperation_IfModelIncompatible()
        {
            // Arrange
            var expectedMessage = "The model item passed into the ViewDataDictionary is of type 'System.Int32', " +
                "but this ViewDataDictionary instance requires a model item of type 'System.String'.";
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary<int>(metadataProvider)
            {
                Model = 23,
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new ViewDataDictionary<string>(source));
            Assert.Equal(expectedMessage, exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => new ViewDataDictionary<string>(source, model: 24));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void ModelSetters_AcceptCompatibleValue()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData1 = new ViewDataDictionary<int>(metadataProvider);
            var viewData2 = new ViewDataDictionary<int>(viewData1);
            var viewData3 = new ViewDataDictionary<int>(viewData2, model: null);
            var viewData4 = new ViewDataDictionary(viewData3);

            // Act
            viewData1.Model = 23;
            viewData2.Model = 24;
            viewData3.Model = 25;
            viewData4.Model = 26;

            // Assert
            Assert.Equal(23, viewData1.Model);
            Assert.Equal(24, viewData2.Model);
            Assert.Equal(25, viewData3.Model);
            Assert.Equal(26, viewData4.Model);
        }

        [Fact]
        public void ModelSetters_AcceptNullValue()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData1 = new ViewDataDictionary<string>(metadataProvider);
            var viewData2 = new ViewDataDictionary<string>(viewData1);
            var viewData3 = new ViewDataDictionary<string>(viewData2, model: null);
            var viewData4 = new ViewDataDictionary(viewData3);

            // Act
            viewData1.Model = null;
            viewData2.Model = null;
            viewData3.Model = null;
            viewData4.Model = null;

            // Assert
            Assert.Null(viewData1.Model);
            Assert.Null(viewData2.Model);
            Assert.Null(viewData3.Model);
            Assert.Null(viewData4.Model);
        }

        [Fact]
        public void ModelSetters_ThrowInvalidOperation_IfModelIncompatible()
        {
            // Arrange
            var expectedMessage = "The model item passed into the ViewDataDictionary is of type 'System.Int32', " +
                "but this ViewDataDictionary instance requires a model item of type 'System.String'.";
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData1 = (ViewDataDictionary)new ViewDataDictionary<string>(metadataProvider);
            var viewData2 = (ViewDataDictionary)new ViewDataDictionary<string>(viewData1);
            var viewData3 = (ViewDataDictionary)new ViewDataDictionary<string>(viewData2, model: null);
            var viewData4 = new ViewDataDictionary(viewData3);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => viewData1.Model = 23);
            Assert.Equal(expectedMessage, exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => viewData2.Model = 24);
            Assert.Equal(expectedMessage, exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => viewData3.Model = 25);
            Assert.Equal(expectedMessage, exception.Message);

            // Non-generic ViewDataDictionary maintains type restrictions of source with 1-parameter constructor.
            exception = Assert.Throws<InvalidOperationException>(() => viewData4.Model = 26);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void ModelSetters_ThrowInvalidOperation_IfModelNullAndTModelNonNullable()
        {
            // Arrange
            var expectedMessage = "The model item passed is null, " +
                "but this ViewDataDictionary instance requires a non-null model item of type 'System.Int32'.";
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData1 = (ViewDataDictionary)new ViewDataDictionary<int>(metadataProvider);
            var viewData2 = (ViewDataDictionary)new ViewDataDictionary<int>(viewData1);
            var viewData3 = (ViewDataDictionary)new ViewDataDictionary<int>(viewData2, model: null);
            var viewData4 = new ViewDataDictionary(viewData3);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => viewData1.Model = null);
            Assert.Equal(expectedMessage, exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => viewData2.Model = null);
            Assert.Equal(expectedMessage, exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => viewData3.Model = null);
            Assert.Equal(expectedMessage, exception.Message);

            // Non-generic ViewDataDictionary maintains type restrictions of source with 1-parameter constructor.
            exception = Assert.Throws<InvalidOperationException>(() => viewData4.Model = null);
            Assert.Equal(expectedMessage, exception.Message);
        }

        private class TestModel
        {
        }

        private class SupremeTestModel : TestModel
        {
        }
    }
}