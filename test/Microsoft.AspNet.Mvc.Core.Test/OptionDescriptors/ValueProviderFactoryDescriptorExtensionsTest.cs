// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ValueProviderFactoryDescriptorExtensionsTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ValueProviderFactoryDescriptor>
            {
                new ValueProviderFactoryDescriptor(Mock.Of<IValueProviderFactory>()),
                new ValueProviderFactoryDescriptor(Mock.Of<IValueProviderFactory>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index",
                                                       () => collection.Insert(index, typeof(IValueProviderFactory)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ValueProviderFactoryDescriptor>
            {
                new ValueProviderFactoryDescriptor(Mock.Of<IValueProviderFactory>()),
                new ValueProviderFactoryDescriptor(Mock.Of<IValueProviderFactory>())
            };
            var valueProviderFactory = Mock.Of<IValueProviderFactory>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, valueProviderFactory));
        }

        [InlineData]
        public void ValueProviderFactoryDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var valueProviderFactory = Mock.Of<IValueProviderFactory>();
            var type = typeof(TestValueProviderFactory);
            var collection = new List<ValueProviderFactoryDescriptor>();

            // Act
            collection.Add(valueProviderFactory);
            collection.Insert(0, type);

            // Assert
            Assert.Equal(2, collection.Count);
            Assert.IsType<TestValueProviderFactory>(collection[0].Instance);
            Assert.Same(valueProviderFactory, collection[0].Instance);
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
