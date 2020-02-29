// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class FormFileModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(IFormCollection))]
        [InlineData(typeof(TestClass))]
        [InlineData(typeof(IList<int>))]
        public void Create_ForUnsupportedTypes_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new FormFileModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(typeof(IFormFile))]
        [InlineData(typeof(IFormFile[]))]
        [InlineData(typeof(IFormFileCollection))]
        [InlineData(typeof(IEnumerable<IFormFile>))]
        [InlineData(typeof(Collection<IFormFile>))]
        public void Create_ForSupportedTypes_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new FormFileModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<FormFileModelBinder>(result);
        }

        private class TestClass
        {
        }
    }
}
