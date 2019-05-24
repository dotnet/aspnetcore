// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class KeyValuePairModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Person))]
        [InlineData(typeof(KeyValuePair<string, int>?))]
        [InlineData(typeof(KeyValuePair<string, int>[]))]
        public void Create_ForNonKeyValuePair_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new KeyValuePairModelBinderProvider();

            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_ForKeyValuePair_ReturnsBinder()
        {
            // Arrange
            var provider = new KeyValuePairModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(KeyValuePair<string, int>));
            context.OnCreatingBinder(m =>
            {
                if (m.ModelType == typeof(string) || m.ModelType == typeof(int))
                {
                    return Mock.Of<IModelBinder>();
                }
                else
                {
                    Assert.False(true, "Not the right model type");
                    return null;
                }
            });

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<KeyValuePairModelBinder<string, int>>(result);
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
