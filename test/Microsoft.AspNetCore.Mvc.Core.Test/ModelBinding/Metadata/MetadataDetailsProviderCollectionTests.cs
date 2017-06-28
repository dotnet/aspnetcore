// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public class MetadataDetailsProviderCollectionTests
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new MetadataDetailsProviderCollection
            {
                new FooMetadataDetailsProvider(),
                new BarMetadataDetailsProvider(),
                new FooMetadataDetailsProvider()
            };

            // Act
            collection.RemoveType(typeof(FooMetadataDetailsProvider));

            // Assert
            var provider = Assert.Single(collection);
            Assert.IsType<BarMetadataDetailsProvider>(provider);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new MetadataDetailsProviderCollection
            {
                new FooMetadataDetailsProvider(),
                new BarMetadataDetailsProvider(),
                new FooMetadataDetailsProvider()
            };

            // Act
            collection.RemoveType<FooMetadataDetailsProvider>();

            // Assert
            var provider = Assert.Single(collection);
            Assert.IsType<BarMetadataDetailsProvider>(provider);
        }

        private class FooMetadataDetailsProvider : IMetadataDetailsProvider
        {
        }

        private class BarMetadataDetailsProvider : IMetadataDetailsProvider
        {
        }
    }
}
