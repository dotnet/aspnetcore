// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class ModelBinderDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotIModelBinder()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.ModelBinding.IModelBinder'.";
            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ModelBinderDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSetsOptionType()
        {
            // Arrange
            var type = typeof(TestModelBinder);

            // Act
            var descriptor = new ModelBinderDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSetsInstanceeAndOptionType()
        {
            // Arrange
            var viewEngine = new TestModelBinder();

            // Act
            var descriptor = new ModelBinderDescriptor(viewEngine);

            // Assert
            Assert.Same(viewEngine, descriptor.Instance);
            Assert.Equal(viewEngine.GetType(), descriptor.OptionType);
        }

        private class TestModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                throw new NotImplementedException();
            }
        }
    }
}