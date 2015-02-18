// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering.Expressions
{
    public class ExpressionMetadataProviderTest
    {
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