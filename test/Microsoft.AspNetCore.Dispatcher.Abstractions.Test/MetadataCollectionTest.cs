// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class MetadataCollectionTest
    {
        [Fact]
        public void GetMetadata_ReturnsLastMatch()
        {
            // Arrange
            var items = new object[]
            {
                new AuthNMetadata(),
                new AuthZMetadata(),
                new AuthNMetadata(),
            };

            var collection = new MetadataCollection(items);

            // Act
            var result = collection.GetMetadata<AuthNMetadata>();

            // Assert
            Assert.Same(items[2], result);
        }

        [Fact]
        public void GetOrderedMetadata_ReturnsAllMatches()
        {
            // Arrange
            var items = new object[]
            {
                new AuthNMetadata(),
                new AuthZMetadata(),
                new AuthNMetadata(),
            };

            var collection = new MetadataCollection(items);

            // Act
            var result = collection.GetOrderedMetadata<AuthNMetadata>();

            // Assert
            Assert.Equal(new object[] { items[0], items[2] }, result);
        }

        [Fact]
        public void GetEnumerator_IncludesAllItems()
        {
            // Arrange
            var items = new object[]
            {
                new AuthNMetadata(),
                new AuthZMetadata(),
                new AuthNMetadata(),
            };

            var collection = new MetadataCollection(items);

            // Act
            var result = collection.ToArray();

            // Assert
            Assert.Equal(items, result);
        }

        private class AuthNMetadata
        {
        }

        private class AuthZMetadata
        {
        }
    }
}
