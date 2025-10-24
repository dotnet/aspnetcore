// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Builder;

public class OpaqueRedirectionTest
{
    [Fact]  
    public void GetBlazorOpaqueRedirectionEndpoint_ContainsComponentFrameworkEndpointMetadata()
    {
        // Arrange & Act
        var endpointBuilder = OpaqueRedirection.GetBlazorOpaqueRedirectionEndpoint();
        var endpoint = endpointBuilder.Build();

        // Assert
        var metadata = endpoint.Metadata.GetMetadata<ComponentFrameworkEndpointMetadata>();
        Assert.NotNull(metadata);
    }

    [Fact]
    public void GetBlazorOpaqueRedirectionEndpoint_HasCorrectDisplayName()
    {
        // Arrange & Act
        var endpointBuilder = OpaqueRedirection.GetBlazorOpaqueRedirectionEndpoint();
        var endpoint = endpointBuilder.Build();

        // Assert
        Assert.Equal("Blazor Opaque Redirection", endpoint.DisplayName);
    }

    [Fact]
    public void GetBlazorOpaqueRedirectionEndpoint_HasHttpGetMethod()
    {
        // Arrange & Act
        var endpointBuilder = OpaqueRedirection.GetBlazorOpaqueRedirectionEndpoint();
        var endpoint = endpointBuilder.Build();

        // Assert
        var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        Assert.NotNull(httpMethodMetadata);
        Assert.Contains(HttpMethods.Get, httpMethodMetadata.HttpMethods);
    }
}