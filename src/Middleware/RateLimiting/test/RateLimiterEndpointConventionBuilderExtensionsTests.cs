// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimiterEndpointConventionBuilderExtensionsTests : LoggedTest
{
    [Fact]
    public void RequireRateLimiting_Name_MetadataAdded()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        testConventionBuilder.RequireRateLimiting("TestPolicyName");

        // Assert
        var addEnableRateLimitingAttribute = Assert.Single(testConventionBuilder.Conventions);

        var endpointModel = new TestEndpointBuilder();
        addEnableRateLimitingAttribute(endpointModel);
        var endpoint = endpointModel.Build();

        var metadata = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        Assert.NotNull(metadata);
        Assert.Equal("TestPolicyName", metadata.PolicyName);
        Assert.Null(metadata.Policy);
    }

    [Fact]
    public void RequireRateLimiting_Policy_MetadataAdded()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        testConventionBuilder.RequireRateLimiting(new TestRateLimiterPolicy("myKey", 404, false));

        // Assert
        var addEnableRateLimitingAttribute = Assert.Single(testConventionBuilder.Conventions);

        var endpointBuilder = new TestEndpointBuilder();
        addEnableRateLimitingAttribute(endpointBuilder);
        var endpoint = endpointBuilder.Build();

        var metadata = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Policy);
        Assert.Null(metadata.PolicyName);
    }

    [Fact]
    public void DisableRateLimiting_MetadataAdded()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        testConventionBuilder.DisableRateLimiting();

        // Assert
        var addDisableRateLimitingAttribute = Assert.Single(testConventionBuilder.Conventions);

        var endpointModel = new TestEndpointBuilder();
        addDisableRateLimitingAttribute(endpointModel);
        var endpoint = endpointModel.Build();

        var metadata = endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>();
        Assert.NotNull(metadata);
    }

    private class TestEndpointBuilder : EndpointBuilder
    {
        public override Endpoint Build()
        {
            return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
        }
    }

    private class TestEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public IList<Action<EndpointBuilder>> Conventions { get; } = new List<Action<EndpointBuilder>>();

        public void Add(Action<EndpointBuilder> convention)
        {
            Conventions.Add(convention);
        }
    }
}
