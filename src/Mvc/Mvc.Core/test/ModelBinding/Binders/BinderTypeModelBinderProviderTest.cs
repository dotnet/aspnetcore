// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class BinderTypeModelBinderProviderTest
    {
        [Fact]
        public void Create_WhenBinderTypeIsNull_ReturnsNull()
        {
            // Arrange
            var provider = new BinderTypeModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(Person));

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_WhenBinderTypeIsSet_ReturnsBinder()
        {
            // Arrange
            var provider = new BinderTypeModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BinderType = typeof(NullModelBinder);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<BinderTypeModelBinder>(result);
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private class NullModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                return Task.FromResult(0);
            }
        }
    }
}
