// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

public class EndpointMetadataComparerTest
{
    [Fact]
    public void Compare_EndpointWithMetadata_MoreSpecific()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { new TestMetadata(), }), "test1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { }), "test2");

        // Act
        var result = EndpointMetadataComparer<TestMetadata>.Default.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Compare_EndpointWithMetadata_ReverseOrder_MoreSpecific()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { }), "test1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { new TestMetadata(), }), "test2");

        // Act
        var result = EndpointMetadataComparer<TestMetadata>.Default.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Compare_BothEndpointsWithMetadata_Equal()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { new TestMetadata(), }), "test1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { new TestMetadata(), }), "test2");

        // Act
        var result = EndpointMetadataComparer<TestMetadata>.Default.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_BothEndpointsWithoutMetadata_Equal()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { }), "test1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { }), "test2");

        // Act
        var result = EndpointMetadataComparer<TestMetadata>.Default.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Sort_EndpointWithMetadata_FirstInList()
    {
        // Arrange
        var endpoint1 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { new TestMetadata(), }), "test1");
        var endpoint2 = new Endpoint(TestConstants.EmptyRequestDelegate, new EndpointMetadataCollection(new object[] { }), "test2");

        var list = new List<Endpoint>() { endpoint2, endpoint1, };

        // Act
        list.Sort(EndpointMetadataComparer<TestMetadata>.Default);

        // Assert
        Assert.Collection(
            list,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
    }

    private class TestMetadata
    {
    }
}
