// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public class ModelValidatorProviderCollectionTests
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new ModelValidatorProviderCollection
            {
                new FooModelValidatorProvider(),
                new BarModelValidatorProvider(),
                new FooModelValidatorProvider()
            };

            // Act
            collection.RemoveType(typeof(FooModelValidatorProvider));

            // Assert
            var provider = Assert.Single(collection);
            Assert.IsType<BarModelValidatorProvider>(provider);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new ModelValidatorProviderCollection
            {
                new FooModelValidatorProvider(),
                new BarModelValidatorProvider(),
                new FooModelValidatorProvider()
            };

            // Act
            collection.RemoveType<FooModelValidatorProvider>();

            // Assert
            var provider = Assert.Single(collection);
            Assert.IsType<BarModelValidatorProvider>(provider);
        }

        private class FooModelValidatorProvider : IModelValidatorProvider
        {
            public void CreateValidators(ModelValidatorProviderContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class BarModelValidatorProvider : IModelValidatorProvider
        {
            public void CreateValidators(ModelValidatorProviderContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
