// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Custom.Http.Rule;
using Grpc.Shared;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class ServiceDescriptorHelpersTests
{
    [Fact]
    public void TryGetHttpRule_CustomHttpRule_Success()
    {
        // Arrange
        var method = HelloService.Descriptor.Methods.Single(m => m.Name == "Say");

        // Act
        Assert.True(ServiceDescriptorHelpers.TryGetHttpRule(method, out var httpRule));

        // Assert
        Assert.Equal("/say", httpRule.Post);
        Assert.Equal("*", httpRule.Body);
    }
}
