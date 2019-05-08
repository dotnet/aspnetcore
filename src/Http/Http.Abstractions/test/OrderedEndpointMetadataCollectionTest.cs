// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Abstractions.Tests
{
    public class OrderedEndpointMetadataCollectionTest
    {
        [Fact]
        public void Constructor_Enumeration_ContainsValues()
        {
            // Arrange & Act
            var metadata = new OrderedEndpointMetadataCollection<string>(new string[]
            {
                "1",
                "2",
                "3",
            });

            // Assert
            Assert.Equal(3, metadata.Count);

            Assert.Collection(metadata,
                value => Assert.Equal("1", value),
                value => Assert.Equal("2", value),
                value => Assert.Equal("3", value));
        }

        [Fact]
        public void Constructor_Loop_ContainsValues()
        {
            // Arrange & Act
            var metadata = new OrderedEndpointMetadataCollection<string>(new string[]
            {
                "1",
                "2",
                "3",
            });

            // Assert
            Assert.Equal(3, metadata.Count);
            for (var i = 0; i < metadata.Count; i++)
            {
                Assert.Equal((i + 1).ToString(), metadata[i]);
            }
        }
    }
}
