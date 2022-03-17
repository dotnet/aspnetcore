// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching;

public class RouteEndpointComparerTest
{
    [Fact]
    public void Compare_PrefersOrder_IfDifferent()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/", order: 1);
        var endpoint2 = CreateEndpoint("/api/foo", order: -1);

        var comparer = CreateComparer();

        // Act
        var result = comparer.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Compare_PrefersPrecedence_IfOrderIsSame()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/api/foo", order: 1);
        var endpoint2 = CreateEndpoint("/", order: 1);

        var comparer = CreateComparer();

        // Act
        var result = comparer.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Compare_PrefersPolicy_IfPrecedenceIsSame()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/", order: 1);

        var comparer = CreateComparer(new TestMetadata1Policy());

        // Act
        var result = comparer.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Compare_PrefersSecondPolicy_IfFirstPolicyIsSame()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/", order: 1, new TestMetadata1(), new TestMetadata2());

        var comparer = CreateComparer(new TestMetadata1Policy(), new TestMetadata2Policy());

        // Act
        var result = comparer.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Compare_PrefersTemplate_IfOtherCriteriaIsSame()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/foo", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/bar", order: 1, new TestMetadata1());

        var comparer = CreateComparer(new TestMetadata1Policy(), new TestMetadata2Policy());

        // Act
        var result = comparer.Compare(endpoint1, endpoint2);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void Compare_ReturnsZero_WhenIdentical()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/foo", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/foo", order: 1, new TestMetadata1());

        var comparer = CreateComparer(new TestMetadata1Policy(), new TestMetadata2Policy());

        // Act
        var result = comparer.Compare(endpoint1, endpoint2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Equals_NotEqual_IfOrderDifferent()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/", order: 1);
        var endpoint2 = CreateEndpoint("/api/foo", order: -1);

        var comparer = CreateComparer();

        // Act
        var result = comparer.Equals(endpoint1, endpoint2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_NotEqual_IfPrecedenceDifferent()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/api/foo", order: 1);
        var endpoint2 = CreateEndpoint("/", order: 1);

        var comparer = CreateComparer();

        // Act
        var result = comparer.Equals(endpoint1, endpoint2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_NotEqual_IfFirstPolicyDifferent()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/", order: 1);

        var comparer = CreateComparer(new TestMetadata1Policy());

        // Act
        var result = comparer.Equals(endpoint1, endpoint2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_NotEqual_IfSecondPolicyDifferent()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/", order: 1, new TestMetadata1(), new TestMetadata2());

        var comparer = CreateComparer(new TestMetadata1Policy(), new TestMetadata2Policy());

        // Act
        var result = comparer.Equals(endpoint1, endpoint2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_Equals_WhenTemplateIsDifferent()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/foo", order: 1, new TestMetadata1());
        var endpoint2 = CreateEndpoint("/bar", order: 1, new TestMetadata1());

        var comparer = CreateComparer(new TestMetadata1Policy(), new TestMetadata2Policy());

        // Act
        var result = comparer.Equals(endpoint1, endpoint2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Sort_MoreSpecific_FirstInList()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/foo", order: -1);
        var endpoint2 = CreateEndpoint("/bar/{baz}", order: -1);
        var endpoint3 = CreateEndpoint("/bar", order: 0, new TestMetadata1());
        var endpoint4 = CreateEndpoint("/foo", order: 0, new TestMetadata2());
        var endpoint5 = CreateEndpoint("/foo", order: 0);
        var endpoint6 = CreateEndpoint("/a{baz}", order: 0, new TestMetadata1(), new TestMetadata2());
        var endpoint7 = CreateEndpoint("/bar{baz}", order: 0, new TestMetadata1(), new TestMetadata2());

        // Endpoints listed in reverse of the desired order.
        var list = new List<RouteEndpoint>() { endpoint7, endpoint6, endpoint5, endpoint4, endpoint3, endpoint2, endpoint1, };

        var comparer = CreateComparer(new TestMetadata1Policy(), new TestMetadata2Policy());

        // Act
        list.Sort(comparer);

        // Assert
        Assert.Collection(
            list,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e),
            e => Assert.Same(endpoint3, e),
            e => Assert.Same(endpoint4, e),
            e => Assert.Same(endpoint5, e),
            e => Assert.Same(endpoint6, e),
            e => Assert.Same(endpoint7, e));
    }

    [Fact]
    public void Compare_PatternOrder_OrdinalIgnoreCaseSort()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/I", order: 0);
        var endpoint2 = CreateEndpoint("/i", order: 0);
        var endpoint3 = CreateEndpoint("/\u0131", order: 0); // Turkish lowercase i

        var list = new List<RouteEndpoint>() { endpoint1, endpoint2, endpoint3 };

        var comparer = CreateComparer();

        // Act
        list.Sort(comparer);

        // Assert
        Assert.Collection(
            list,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e),
            e => Assert.Same(endpoint3, e));
    }

    private static RouteEndpoint CreateEndpoint(string template, int order, params object[] metadata)
    {
        return new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse(template),
            order,
            new EndpointMetadataCollection(metadata),
            "test: " + template);
    }

    private static EndpointComparer CreateComparer(params IEndpointComparerPolicy[] policies)
    {
        return new EndpointComparer(policies);
    }

    private class TestMetadata1
    {
    }

    private class TestMetadata1Policy : IEndpointComparerPolicy
    {
        public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata1>.Default;
    }

    private class TestMetadata2
    {
    }

    private class TestMetadata2Policy : IEndpointComparerPolicy
    {
        public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata2>.Default;
    }
}
