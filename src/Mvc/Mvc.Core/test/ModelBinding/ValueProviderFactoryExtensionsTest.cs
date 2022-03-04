// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ValueProviderFactoryExtensionsTest
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IValueProviderFactory>
            {
                new FooValueProviderFactory(),
                new BarValueProviderFactory(),
                new FooValueProviderFactory()
            };

            // Act
            list.RemoveType(typeof(FooValueProviderFactory));

            // Assert
            var factory = Assert.Single(list);
            Assert.IsType<BarValueProviderFactory>(factory);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IValueProviderFactory>
            {
                new FooValueProviderFactory(),
                new BarValueProviderFactory(),
                new FooValueProviderFactory()
            };

            // Act
            list.RemoveType<FooValueProviderFactory>();

            // Assert
            var factory = Assert.Single(list);
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
