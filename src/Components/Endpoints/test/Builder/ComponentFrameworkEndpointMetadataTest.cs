// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Builder;

public class ComponentFrameworkEndpointMetadataTest
{
    [Fact]
    public void ComponentFrameworkEndpointMetadata_CanBeCreated()
    {
        // Arrange & Act
        var metadata = new ComponentFrameworkEndpointMetadata();

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void ComponentFrameworkEndpointMetadata_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ComponentFrameworkEndpointMetadata);

        // Assert
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void ComponentFrameworkEndpointMetadata_HasNoPublicProperties()
    {
        // Arrange & Act
        var type = typeof(ComponentFrameworkEndpointMetadata);
        var properties = type.GetProperties();

        // Assert
        Assert.Empty(properties);
    }
}