// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public class ResourceAssetCollectionTest
{
    [Fact]
    public void CanCreateResourceCollection()
    {
        // Arrange
        var resourceAssetCollection = new ResourceAssetCollection([
            new ResourceAsset("image1.jpg",[]),
            ]);

        // Act
        var collectionAsReadOnlyList = resourceAssetCollection as IReadOnlyList<ResourceAsset>;

        // Assert
        Assert.Equal(1, collectionAsReadOnlyList.Count);
        Assert.Equal("image1.jpg", collectionAsReadOnlyList[0].Url);
    }

    [Fact]
    public void CanResolveFingerprintedResources()
    {
        // Arrange
        var resourceAssetCollection = new ResourceAssetCollection([
            new ResourceAsset(
                "image1.fingerprint.jpg",
                [new ResourceAssetProperty("label", "image1.jpg")]),
            ]);

        // Act
        var resolvedUrl = resourceAssetCollection["image1.jpg"];

        // Assert
        Assert.Equal("image1.fingerprint.jpg", resolvedUrl);
    }

    [Fact]
    public void ResolvingNoFingerprintedResourcesReturnsSameUrl()
    {
        // Arrange
        var resourceAssetCollection = new ResourceAssetCollection([
            new ResourceAsset("image1.jpg",[])]);

        // Act
        var resolvedUrl = resourceAssetCollection["image1.jpg"];

        // Assert
        Assert.Equal("image1.jpg", resolvedUrl);
    }

    [Fact]
    public void ResolvingNonExistentResourceReturnsSameUrl()
    {
        // Arrange
        var resourceAssetCollection = new ResourceAssetCollection([
            new ResourceAsset("image1.jpg",[])]);

        // Act
        var resolvedUrl = resourceAssetCollection["image2.jpg"];

        // Assert
        Assert.Equal("image2.jpg", resolvedUrl);
    }

    [Fact]
    public void CanDetermineContentSpecificUrls()
    {
        // Arrange
        var resourceAssetCollection = new ResourceAssetCollection([
            new ResourceAsset("image1.jpg",[]),
            new ResourceAsset(
                "image2.fingerprint.jpg",
                [new ResourceAssetProperty("label", "image2.jpg")]),
            ]);

        // Act
        var isContentSpecificUrl1 = resourceAssetCollection.IsContentSpecificUrl("image1.jpg");
        var isContentSpecificUrl2 = resourceAssetCollection.IsContentSpecificUrl("image2.fingerprint.jpg");

        // Assert
        Assert.False(isContentSpecificUrl1);
        Assert.True(isContentSpecificUrl2);
    }
}
