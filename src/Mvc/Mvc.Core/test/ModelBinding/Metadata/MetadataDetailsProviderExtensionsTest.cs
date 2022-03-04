// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public class MetadataDetailsProviderExtensionsTest
    {
        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IMetadataDetailsProvider>
            {
                new FooMetadataDetailsProvider(),
                new BarMetadataDetailsProvider(),
                new FooMetadataDetailsProvider()
            };

            // Act
            list.RemoveType(typeof(FooMetadataDetailsProvider));

            // Assert
            var provider = Assert.Single(list);
            Assert.IsType<BarMetadataDetailsProvider>(provider);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IMetadataDetailsProvider>
            {
                new FooMetadataDetailsProvider(),
                new BarMetadataDetailsProvider(),
                new FooMetadataDetailsProvider()
            };

            // Act
            list.RemoveType<FooMetadataDetailsProvider>();

            // Assert
            var provider = Assert.Single(list);
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
