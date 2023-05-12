// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging.Tests;

public class HttpLoggingEndpointConventionBuilderTests
{
    [Fact]
    public void WithHttpLogging_SetsMetadata()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();
        var loggingFields = HttpLoggingFields.RequestScheme | HttpLoggingFields.RequestPath;
        var requestBodyLogLimit = 22;
        var responseBodyLogLimit = 94;

        // Act
        testConventionBuilder.WithHttpLogging(loggingFields, requestBodyLogLimit, responseBodyLogLimit);

        // Assert
        var httpLogingAttribute = Assert.Single(testConventionBuilder.Conventions);

        var endpointModel = new TestEndpointBuilder();
        httpLogingAttribute(endpointModel);
        var endpoint = endpointModel.Build();

        var metadata = endpoint.Metadata.GetMetadata<HttpLoggingAttribute>();
        Assert.NotNull(metadata);
        Assert.Equal(requestBodyLogLimit, metadata.RequestBodyLogLimit);
        Assert.Equal(responseBodyLogLimit, metadata.ResponseBodyLogLimit);
        Assert.Equal(loggingFields, metadata.LoggingFields);
    }

    [Fact]
    public void WithHttpLogging_ThrowsForInvalidLimits()
    {
        // Arrange
        var testConventionBuilder = new TestEndpointConventionBuilder();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            testConventionBuilder.WithHttpLogging(HttpLoggingFields.None, requestBodyLogLimit: -1));
        Assert.Equal("requestBodyLogLimit", ex.ParamName);

        ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            testConventionBuilder.WithHttpLogging(HttpLoggingFields.None, responseBodyLogLimit: -1));
        Assert.Equal("responseBodyLogLimit", ex.ParamName);
    }
}

internal class TestEndpointBuilder : EndpointBuilder
{
    public override Endpoint Build()
    {
        return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
    }
}

internal class TestEndpointConventionBuilder : IEndpointConventionBuilder
{
    public IList<Action<EndpointBuilder>> Conventions { get; } = new List<Action<EndpointBuilder>>();

    public void Add(Action<EndpointBuilder> convention)
    {
        Conventions.Add(convention);
    }

    public TestEndpointConventionBuilder ApplyToEndpoint(EndpointBuilder endpoint)
    {
        foreach (var convention in Conventions)
        {
            convention(endpoint);
        }

        return this;
    }
}
