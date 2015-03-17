// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ModelValidatorProviderDescriptorExtensionsTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ModelValidatorProviderDescriptor>
            {
                new ModelValidatorProviderDescriptor(Mock.Of<IModelValidatorProvider>()),
                new ModelValidatorProviderDescriptor(Mock.Of<IModelValidatorProvider>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index",
                                                       () => collection.Insert(index, typeof(IModelValidatorProvider)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ModelValidatorProviderDescriptor>
            {
                new ModelValidatorProviderDescriptor(Mock.Of<IModelValidatorProvider>()),
                new ModelValidatorProviderDescriptor(Mock.Of<IModelValidatorProvider>())
            };
            var valueProviderFactory = Mock.Of<IModelValidatorProvider>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, valueProviderFactory));
        }

        [InlineData]
        public void ModelValidatorProviderDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var provider1 = Mock.Of<IModelValidatorProvider>();
            var provider2 = Mock.Of<IModelValidatorProvider>();
            var type1 = typeof(DefaultModelValidatorProvider);
            var type2 = typeof(DataAnnotationsModelValidatorProvider);
            var collection = new List<ModelValidatorProviderDescriptor>();

            // Act
            collection.Add(provider2);
            collection.Insert(0, provider1);
            collection.Add(type2);
            collection.Insert(2, type1);

            // Assert
            Assert.Equal(4, collection.Count);
            Assert.Same(provider1, collection[0].Instance);
            Assert.Same(provider2, collection[1].Instance);
            Assert.IsType<DefaultModelValidatorProvider>(collection[2].OptionType);
            Assert.IsType<DataAnnotationsModelValidatorProvider>(collection[3].OptionType);
        }
    }
}
