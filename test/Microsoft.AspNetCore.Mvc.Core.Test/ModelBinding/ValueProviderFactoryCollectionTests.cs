// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ValueProviderFactoryCollectionTests
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new ValueProviderFactoryCollection
            {
                new FooValueProviderFactory(),
                new BarValueProviderFactory(),
                new FooValueProviderFactory()
            };

            // Act
            collection.RemoveType(typeof(FooValueProviderFactory));

            // Assert
            var factory = Assert.Single(collection);
            Assert.IsType<BarValueProviderFactory>(factory);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new ValueProviderFactoryCollection
            {
                new FooValueProviderFactory(),
                new BarValueProviderFactory(),
                new FooValueProviderFactory()
            };

            // Act
            collection.RemoveType<FooValueProviderFactory>();

            // Assert
            var factory = Assert.Single(collection);
            Assert.IsType<BarValueProviderFactory>(factory);
        }

        private class FooValueProviderFactory : IValueProviderFactory
        {
            public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class BarValueProviderFactory : IValueProviderFactory
        {
            public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
