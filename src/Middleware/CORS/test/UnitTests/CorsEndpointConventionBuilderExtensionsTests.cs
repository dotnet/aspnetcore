// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsEndpointConventionBuilderExtensionsTests
{
    [Fact]
    public void RequireCors_Name_MetadataAdded()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        testConventionBuilder.RequireCors("TestPolicyName");

        // Assert
        var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

        var endpointModel = new TestEndpointBuilder();
        addCorsPolicy(endpointModel);
        var endpoint = endpointModel.Build();

        var metadata = endpoint.Metadata.GetMetadata<IEnableCorsAttribute>();
        Assert.NotNull(metadata);
        Assert.Equal("TestPolicyName", metadata.PolicyName);
    }

    [Fact]
    public void RequireCors_Policy_MetadataAdded()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        testConventionBuilder.RequireCors(builder => builder.AllowAnyOrigin());

        // Assert
        var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

        var endpointBuilder = new TestEndpointBuilder();
        addCorsPolicy(endpointBuilder);
        var endpoint = endpointBuilder.Build();

        var metadata = endpoint.Metadata.GetMetadata<ICorsPolicyMetadata>();
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Policy);
        Assert.True(metadata.Policy.AllowAnyOrigin);
    }

    [Fact]
    public void RequireCors_NoParameter_MetadataAdded()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        testConventionBuilder.RequireCors();

        // Assert
        var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

        var endpointModel = new TestEndpointBuilder();
        addCorsPolicy(endpointModel);
        var endpoint = endpointModel.Build();

        var metadata = endpoint.Metadata.GetMetadata<IEnableCorsAttribute>();
        Assert.NotNull(metadata);
        Assert.Null(metadata.PolicyName);
    }

    [Fact]
    public void RequireCors_ChainedCall_ReturnedBuilderIsDerivedType()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act
        var builder = testConventionBuilder.RequireCors("TestPolicyName");

        // Assert
        Assert.True(builder.TestProperty);
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
        public bool TestProperty { get; } = true;

        public void Add(Action<EndpointBuilder> convention)
        {
            Conventions.Add(convention);
        }
    }
}
