// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class ResourceCollectionProviderTest
{
    [Fact]
    public async Task GetResourceCollection_WhenUrlIsNull_ReturnsEmpty()
    {
        // Arrange
        var jsRuntime = Mock.Of<IJSRuntime>();
        var provider = new ResourceCollectionProvider(jsRuntime);

        // Act
        var result = await provider.GetResourceCollection();

        // Assert
        Assert.Same(ResourceAssetCollection.Empty, result);
    }

    [Fact]
    public async Task GetResourceCollection_WhenImportFails_ThrowsDescriptiveError()
    {
        // Arrange
        var url = "/_framework/resource-collection.abc123.js";
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("Failed to fetch dynamically imported module"));

        var provider = new ResourceCollectionProvider(jsRuntime.Object);
        provider.ResourceCollectionUrl = url;

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetResourceCollection());

        // Assert
        Assert.Contains($"Failed to load the Blazor resource collection from '{url}'", ex.Message);
        Assert.Contains("integrity", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ex.InnerException);
        Assert.IsType<JSException>(ex.InnerException);
    }

    [Fact]
    public async Task GetResourceCollection_WhenGetFails_ThrowsDescriptiveError()
    {
        // Arrange
        var url = "/_framework/resource-collection.abc123.js";
        var jsRuntime = new Mock<IJSRuntime>();
        var moduleReference = new Mock<IJSObjectReference>();

        jsRuntime
            .Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleReference.Object);

        moduleReference
            .Setup(m => m.InvokeAsync<ResourceAsset[]>("get", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("An error occurred"));

        var provider = new ResourceCollectionProvider(jsRuntime.Object);
        provider.ResourceCollectionUrl = url;

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetResourceCollection());

        // Assert
        Assert.Contains($"Failed to load the Blazor resource collection from '{url}'", ex.Message);
        Assert.Contains("integrity", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ex.InnerException);
        Assert.IsType<JSException>(ex.InnerException);
    }
}
