// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class ByteArrayModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(TestClass))]
        [InlineData(typeof(IList<byte>))]
        [InlineData(typeof(int[]))]
        public void Create_ForNonByteArrayTypes_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new ByteArrayModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_ForByteArray_ReturnsBinder()
        {
            // Arrange
            var provider = new ByteArrayModelBinderProvider();
            var context = new TestModelBinderProviderContext(typeof(byte[]));

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<ByteArrayModelBinder>(result);
        }

        private class TestClass
        {
        }
    }
}
