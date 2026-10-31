// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

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
