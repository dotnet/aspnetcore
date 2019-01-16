// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointMetadataCollectionTests
    {
        [Fact]
        public void Constructor_Enumeration_ContainsValues()
        {
            // Arrange & Act
            var metadata = new EndpointMetadataCollection(new List<object>
            {
                1,
                2,
                3,
            });

            // Assert
            Assert.Equal(3, metadata.Count);

            Assert.Collection(metadata,
                value => Assert.Equal(1, value),
                value => Assert.Equal(2, value),
                value => Assert.Equal(3, value));
        }

        [Fact]
        public void Constructor_ParamsArray_ContainsValues()
        {
            // Arrange & Act
            var metadata = new EndpointMetadataCollection(1, 2, 3);

            // Assert
            Assert.Equal(3, metadata.Count);

            Assert.Collection(metadata,
                value => Assert.Equal(1, value),
                value => Assert.Equal(2, value),
                value => Assert.Equal(3, value));
        }

        [Fact]
        public void GetMetadata_Match_ReturnsLastMatchingEntry()
        {
            // Arrange
            var items = new object[]
            {
                new Metadata1(),
                new Metadata2(),
                new Metadata3(),
            };

            var metadata = new EndpointMetadataCollection(items);

            // Act
            var result = metadata.GetMetadata<IMetadata5>();

            // Assert
            Assert.Same(items[1], result);
        }

        [Fact]
        public void GetMetadata_NoMatch_ReturnsNull()
        {
            // Arrange
            var items = new object[]
            {
                new Metadata3(),
                new Metadata3(),
                new Metadata3(),
            };

            var metadata = new EndpointMetadataCollection(items);

            // Act
            var result = metadata.GetMetadata<IMetadata5>();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetOrderedMetadata_Match_ReturnsItemsInAscendingOrder()
        {
            // Arrange
            var items = new object[]
            {
                new Metadata1(),
                new Metadata2(),
                new Metadata3(),
            };

            var metadata = new EndpointMetadataCollection(items);

            // Act
            var result = metadata.GetOrderedMetadata<IMetadata5>();

            // Assert
            Assert.Collection(
                result,
                i => Assert.Same(items[0], i),
                i => Assert.Same(items[1], i));
        }

        [Fact]
        public void GetOrderedMetadata_NoMatch_ReturnsEmpty()
        {
            // Arrange
            var items = new object[]
            {
                new Metadata3(),
                new Metadata3(),
                new Metadata3(),
            };

            var metadata = new EndpointMetadataCollection(items);

            // Act
            var result = metadata.GetOrderedMetadata<IMetadata5>();

            // Assert
            Assert.Empty(result);
        }

        private interface IMetadata1 { }
        private interface IMetadata2 { }
        private interface IMetadata3 { }
        private interface IMetadata4 { }
        private interface IMetadata5 { }
        private class Metadata1 : IMetadata1, IMetadata4, IMetadata5 { }
        private class Metadata2 : IMetadata2, IMetadata5 { }
        private class Metadata3 : IMetadata3 { }

    }
}