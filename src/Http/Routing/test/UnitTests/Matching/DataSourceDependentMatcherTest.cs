// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;

namespace Microsoft.AspNetCore.Routing.Matching;

public class DataSourceDependentMatcherTest
{
    [Fact]
    public void Matcher_Initializes_InConstructor()
    {
        // Arrange
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();

        // Act
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        // Assert
        var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
        Assert.Empty(inner.Endpoints);

        Assert.NotNull(lifetime.Cache);
    }

    [Fact]
    public void Matcher_Reinitializes_WhenDataSourceChanges()
    {
        // Arrange
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        var endpoint = new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("a/b/c"),
            0,
            EndpointMetadataCollection.Empty,
            "test");

        // Act
        dataSource.AddEndpoint(endpoint);

        // Assert
        var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
        Assert.Collection(
            inner.Endpoints,
            e => Assert.Same(endpoint, e));
    }

    [Fact]
    public void Matcher_IgnoresUpdate_WhenDisposed()
    {
        // Arrange
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        var endpoint = new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("a/b/c"),
            0,
            EndpointMetadataCollection.Empty,
            "test");

        lifetime.Dispose();

        // Act
        dataSource.AddEndpoint(endpoint);

        // Assert
        var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
        Assert.Empty(inner.Endpoints);
    }

    [Fact]
    public void Matcher_Ignores_NonRouteEndpoint()
    {
        // Arrange
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        var endpoint = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "test");
        dataSource.AddEndpoint(endpoint);

        // Act
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        // Assert
        var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
        Assert.Empty(inner.Endpoints);
    }

    [Fact]
    public void Matcher_Ignores_SuppressedEndpoint()
    {
        // Arrange
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        var endpoint = new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(new SuppressMatchingMetadata()),
            "test");
        dataSource.AddEndpoint(endpoint);

        // Act
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        // Assert
        var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
        Assert.Empty(inner.Endpoints);
    }

    [Fact]
    public void Matcher_UnsuppressedEndpoint_IsUsed()
    {
        // Arrange
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        var endpoint = new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(new SuppressMatchingMetadata(), new EncourageMatchingMetadata()),
            "test");
        dataSource.AddEndpoint(endpoint);

        // Act
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        // Assert
        var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
        Assert.Same(endpoint, Assert.Single(inner.Endpoints));
    }

    [Fact]
    public void Matcher_ThrowsOnDuplicateEndpoints()
    {
        // Arrange
        var expectedError = "Duplicate endpoint name 'Foo' found on '/bar' and '/foo'. Endpoint names must be globally unique.";
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        dataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/foo"),
            0,
            new EndpointMetadataCollection(new EndpointNameMetadata("Foo")),
            "/foo"
        ));
        dataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/bar"),
            0,
            new EndpointMetadataCollection(new EndpointNameMetadata("Foo")),
            "/bar"
        ));

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create));
        Assert.Equal(expectedError, exception.Message);
    }

    [Fact]
    public void Matcher_ThrowsOnDuplicateEndpointsFromMultipleSources()
    {
        // Arrange
        var expectedError = "Duplicate endpoint name 'Foo' found on '/foo2' and '/foo'. Endpoint names must be globally unique.";
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        dataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/foo"),
            0,
            new EndpointMetadataCollection(new EndpointNameMetadata("Foo")),
            "/foo"
        ));
        dataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/bar"),
            0,
            new EndpointMetadataCollection(new EndpointNameMetadata("Bar")),
            "/bar"
        ));
        var anotherDataSource = new DynamicEndpointDataSource();
        anotherDataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/foo2"),
            0,
            new EndpointMetadataCollection(new EndpointNameMetadata("Foo")),
            "/foo2"
        ));

        var compositeDataSource = new CompositeEndpointDataSource(new[] { dataSource, anotherDataSource });

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new DataSourceDependentMatcher(compositeDataSource, lifetime, TestMatcherBuilder.Create));
        Assert.Equal(expectedError, exception.Message);
    }

    [Fact]
    public void Matcher_ThrowsOnDuplicateEndpointAddedLater()
    {
        // Arrange
        var expectedError = "Duplicate endpoint name 'Foo' found on '/bar' and '/foo'. Endpoint names must be globally unique.";
        var dataSource = new DynamicEndpointDataSource();
        var lifetime = new DataSourceDependentMatcher.Lifetime();
        dataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/foo"),
            0,
            new EndpointMetadataCollection(new EndpointNameMetadata("Foo")),
            "/foo"
        ));

        // Act (should be all good since no duplicate has been added yet)
        var matcher = new DataSourceDependentMatcher(dataSource, lifetime, TestMatcherBuilder.Create);

        // Assert that rerunning initializer throws AggregateException
        var exception = Assert.Throws<AggregateException>(
            () => dataSource.AddEndpoint(new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse("/bar"),
                0,
                new EndpointMetadataCollection(new EndpointNameMetadata("Foo")),
                "/bar"
        )));
        Assert.Equal(expectedError, exception.InnerException.Message);
    }

    private class TestMatcherBuilder : MatcherBuilder
    {
        public static Func<MatcherBuilder> Create = () => new TestMatcherBuilder();

        private List<RouteEndpoint> Endpoints { get; } = new List<RouteEndpoint>();

        public override void AddEndpoint(RouteEndpoint endpoint)
        {
            Endpoints.Add(endpoint);
        }

        public override Matcher Build()
        {
            return new TestMatcher() { Endpoints = Endpoints, };
        }
    }

    private class TestMatcher : Matcher
    {
        public IReadOnlyList<RouteEndpoint> Endpoints { get; set; }

        public override Task MatchAsync(HttpContext httpContext)
        {
            throw new NotImplementedException();
        }
    }

    private class EncourageMatchingMetadata : ISuppressMatchingMetadata
    {
        public bool SuppressMatching => false;
    }
}
