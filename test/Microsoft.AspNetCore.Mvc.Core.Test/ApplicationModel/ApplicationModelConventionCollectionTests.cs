// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class ApplicationModelConventionCollectionTests
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new ApplicationModelConventionCollection
            {
                new FooApplicationModelConvention(),
                new BarApplicationModelConvention(),
                new FooApplicationModelConvention()
            };

            // Act
            collection.RemoveType(typeof(FooApplicationModelConvention));

            // Assert
            var convention = Assert.Single(collection);
            Assert.IsType<BarApplicationModelConvention>(convention);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new ApplicationModelConventionCollection
            {
                new FooApplicationModelConvention(),
                new BarApplicationModelConvention(),
                new FooApplicationModelConvention()
            };

            // Act
            collection.RemoveType<FooApplicationModelConvention>();

            // Assert
            var convention = Assert.Single(collection);
            Assert.IsType<BarApplicationModelConvention>(convention);
        }

        private class FooApplicationModelConvention : IApplicationModelConvention
        {
            public void Apply(ApplicationModel application)
            {
                throw new NotImplementedException();
            }
        }

        private class BarApplicationModelConvention : IApplicationModelConvention
        {
            public void Apply(ApplicationModel application)
            {
                throw new NotImplementedException();
            }
        }
    }
}
