// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class FloatingPointTypeModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Calendar))]
        [InlineData(typeof(TestClass))]
        [InlineData(typeof(List<int>))]
        public void Create_ForCollectionOrComplexTypes_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new FloatingPointTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int?))]
        [InlineData(typeof(char))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTime?))]
        public void Create_ForUnsupportedSimpleTypes_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new FloatingPointTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(decimal?))]
        public void Create_ForDecimalTypes_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new FloatingPointTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<DecimalModelBinder>(result);
        }

        [Theory]
        [InlineData(typeof(double))]
        [InlineData(typeof(double?))]
        public void Create_ForDoubleTypes_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new FloatingPointTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<DoubleModelBinder>(result);
        }

        [Theory]
        [InlineData(typeof(float))]
        [InlineData(typeof(float?))]
        public void Create_ForFloatTypes_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new FloatingPointTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<FloatModelBinder>(result);
        }

        private class TestClass
        {
        }
    }
}
