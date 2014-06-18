// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBinderDescriptorExtensionTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ModelBinderDescriptor>
            {
                new ModelBinderDescriptor(Mock.Of<IModelBinder>()),
                new ModelBinderDescriptor(Mock.Of<IModelBinder>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, typeof(IModelBinder)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ModelBinderDescriptor>
            {
                new ModelBinderDescriptor(Mock.Of<IModelBinder>()),
                new ModelBinderDescriptor(Mock.Of<IModelBinder>())
            };
            var binder = Mock.Of<IModelBinder>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, binder));
        }

        [InlineData]
        public void ModelBinderDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var binder1 = Mock.Of<IModelBinder>();
            var binder2 = Mock.Of<IModelBinder>();
            var type1 = typeof(TypeMatchModelBinder);
            var type2 = typeof(TypeConverterModelBinder);
            var collection = new List<ModelBinderDescriptor>();

            // Act
            collection.Add(binder1);
            collection.Insert(1, binder2);
            collection.Add(type1);
            collection.Insert(2, type2);

            // Assert
            Assert.Equal(4, collection.Count);
            Assert.Equal(binder1, collection[0].ModelBinder);
            Assert.Equal(binder2, collection[1].ModelBinder);
            Assert.Equal(type2, collection[2].ModelBinderType);
            Assert.Equal(type1, collection[3].ModelBinderType);
        }
    }
}
#endif