// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public class ModelValidatorProviderExtensionsTest
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IModelValidatorProvider>
            {
                new FooModelValidatorProvider(),
                new BarModelValidatorProvider(),
                new FooModelValidatorProvider()
            };

            // Act
            list.RemoveType(typeof(FooModelValidatorProvider));

            // Assert
            var provider = Assert.Single(list);
            Assert.IsType<BarModelValidatorProvider>(provider);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IModelValidatorProvider>
            {
                new FooModelValidatorProvider(),
                new BarModelValidatorProvider(),
                new FooModelValidatorProvider()
            };

            // Act
            list.RemoveType<FooModelValidatorProvider>();

            // Assert
            var provider = Assert.Single(list);
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
