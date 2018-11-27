// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class SimpleTypeModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Calendar))]
        [InlineData(typeof(TestClass))]
        [InlineData(typeof(List<int>))]
        public void Create_ForCollectionOrComplexTypes_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new SimpleTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTime?))]
        public void Create_ForSimpleTypes_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new SimpleTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<SimpleTypeModelBinder>(result);
        }

        private class TestClass
        {
        }
    }
}
