// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

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
        Assert.Single(collectionAsReadOnlyList);
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

    [Fact]
    public void ResourceAsset_CanSerializeAndDeserialize_WithoutRespectRequiredConstructorParameters()
    {
        // Arrange
        var originalAsset = new ResourceAsset("test-url", null);
        var options = new JsonSerializerOptions { WriteIndented = true };

        // Act
        var json = JsonSerializer.Serialize(originalAsset, options);
        var deserializedAsset = JsonSerializer.Deserialize<ResourceAsset>(json, options);

        // Assert
        Assert.NotNull(deserializedAsset);
        Assert.Equal("test-url", deserializedAsset.Url);
        Assert.Null(deserializedAsset.Properties);
    }

    [Fact]
    public void ResourceAsset_CanSerializeAndDeserialize_WithRespectRequiredConstructorParameters()
    {
        // Arrange
        var originalAsset = new ResourceAsset("test-url", null);
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            RespectRequiredConstructorParameters = true
        };

        // Act
        var json = JsonSerializer.Serialize(originalAsset, options);
        var deserializedAsset = JsonSerializer.Deserialize<ResourceAsset>(json, options);

        // Assert
        Assert.NotNull(deserializedAsset);
        Assert.Equal("test-url", deserializedAsset.Url);
        Assert.Null(deserializedAsset.Properties);
    }

    [Fact] 
    public void ResourceAsset_WithSourceGenerationContext_CanSerializeAndDeserializeWithRespectRequiredConstructorParameters()
    {
        // Arrange - this test simulates the context from ResourceCollectionUrlEndpoint
        var originalAsset = new ResourceAsset("test-url", null);
        var assets = new List<ResourceAsset> { originalAsset };
        
        // Use a custom JsonSerializerOptions that mimics the source-generated context behavior
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = false,
            RespectRequiredConstructorParameters = true
        };

        // Act
        var json = JsonSerializer.Serialize<IReadOnlyList<ResourceAsset>>(assets, options);
        var deserializedAssets = JsonSerializer.Deserialize<IReadOnlyList<ResourceAsset>>(json, options);

        // Assert
        Assert.NotNull(deserializedAssets);
        Assert.Single(deserializedAssets);
        Assert.Equal("test-url", deserializedAssets[0].Url);
        Assert.Null(deserializedAssets[0].Properties);
    }
}
