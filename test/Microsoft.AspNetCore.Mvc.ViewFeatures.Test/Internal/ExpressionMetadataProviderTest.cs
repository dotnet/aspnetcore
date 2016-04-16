// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ExpressionMetadataProviderTest
    {
        [Fact]
        public void FromLambdaExpression_GetsExpectedMetadata_ForIdentityExpression()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel>(provider);

            // Act
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(m => m, viewData, provider);

            // Assert
            Assert.NotNull(explorer);
            Assert.NotNull(explorer.Metadata);
            Assert.Equal(ModelMetadataKind.Type, explorer.Metadata.MetadataKind);
            Assert.Equal(typeof(TestModel), explorer.ModelType);
            Assert.Null(explorer.Model);
        }

        [Fact]
        public void FromLambdaExpression_GetsExpectedMetadata_ForPropertyExpression()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel>(provider);

            // Act
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(m => m.SelectedCategory, viewData, provider);

            // Assert
            Assert.NotNull(explorer);
            Assert.NotNull(explorer.Metadata);
            Assert.Equal(ModelMetadataKind.Property, explorer.Metadata.MetadataKind);
            Assert.Equal(typeof(Category), explorer.ModelType);
            Assert.Null(explorer.Model);
        }

        [Fact]
        public void FromLambdaExpression_GetsExpectedMetadata_ForIndexerExpression()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel[]>(provider);

            // Act
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(m => m[23], viewData, provider);

            // Assert
            Assert.NotNull(explorer);
            Assert.NotNull(explorer.Metadata);
            Assert.Equal(ModelMetadataKind.Type, explorer.Metadata.MetadataKind);
            Assert.Equal(typeof(TestModel), explorer.ModelType);
            Assert.Null(explorer.Model);
        }

        [Fact]
        public void FromLambdaExpression_GetsExpectedMetadata_ForLongerExpression()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel[]>(provider);
            var index = 42;

            // Act
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(
                m => m[index].SelectedCategory.CategoryId,
                viewData,
                provider);

            // Assert
            Assert.NotNull(explorer);
            Assert.NotNull(explorer.Metadata);
            Assert.Equal(ModelMetadataKind.Property, explorer.Metadata.MetadataKind);
            Assert.Equal(typeof(int), explorer.ModelType);
            Assert.Null(explorer.Model);
        }

        [Fact]
        public void FromLambaExpression_SetsContainerAsExpected()
        {
            // Arrange
            var myModel = new TestModel { SelectedCategory = new Category() };
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel>(provider);
            viewData.Model = myModel;

            // Act
            var metadata = ExpressionMetadataProvider.FromLambdaExpression<TestModel, Category>(
                model => model.SelectedCategory,
                viewData,
                provider);

            // Assert
            Assert.Same(myModel, metadata.Container.Model);
        }

        [Theory]
        [InlineData(null, ModelMetadataKind.Type, typeof(TestModel))]
        [InlineData("", ModelMetadataKind.Type, typeof(TestModel))]
        [InlineData("SelectedCategory", ModelMetadataKind.Property, typeof(Category))]
        [InlineData("SelectedCategory.CategoryName", ModelMetadataKind.Type, typeof(string))]
        public void FromStringExpression_GetsExpectedMetadata(
            string expression,
            ModelMetadataKind expectedKind,
            Type expectedType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel>(provider);

            // Act
            var explorer = ExpressionMetadataProvider.FromStringExpression(expression, viewData, provider);

            // Assert
            Assert.NotNull(explorer);
            Assert.NotNull(explorer.Metadata);
            Assert.Equal(expectedKind, explorer.Metadata.MetadataKind);
            Assert.Equal(expectedType, explorer.ModelType);
            Assert.Null(explorer.Model);
        }

        [Fact]
        public void FromStringExpression_SetsContainerAsExpected()
        {
            // Arrange
            var myModel = new TestModel { SelectedCategory = new Category() };
            var provider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary<TestModel>(provider);
            viewData["Object"] = myModel;

            // Act
            var metadata = ExpressionMetadataProvider.FromStringExpression("Object.SelectedCategory",
                                                                           viewData,
                                                                           provider);

            // Assert
            Assert.Same(myModel, metadata.Container.Model);
        }

        private class TestModel
        {
            public Category SelectedCategory { get; set; }
        }

        private class Category
        {
            public int CategoryId { get; set; }

            public string CategoryName { get; set; }
        }
    }
}