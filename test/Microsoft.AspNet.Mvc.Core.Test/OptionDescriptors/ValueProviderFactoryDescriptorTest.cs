// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class ValueProviderFactoryDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotViewEngine()
        {
            // Arrange
            var viewEngineType = typeof(IValueProviderFactory).FullName;
            var type = typeof(string);
            var expected = string.Format("The type '{0}' must derive from '{1}'.",
                                         type.FullName, viewEngineType);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ValueProviderFactoryDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSetsViewEngineType()
        {
            // Arrange
            var type = typeof(TestValueProviderFactory);

            // Act
            var descriptor = new ValueProviderFactoryDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSetsViewEngineAndViewEngineType()
        {
            // Arrange
            var viewEngine = new TestValueProviderFactory();

            // Act
            var descriptor = new ValueProviderFactoryDescriptor(viewEngine);

            // Assert
            Assert.Same(viewEngine, descriptor.Instance);
            Assert.Equal(viewEngine.GetType(), descriptor.OptionType);
        }

        private class TestValueProviderFactory : IValueProviderFactory
        {
            public IValueProvider GetValueProvider(ValueProviderFactoryContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}