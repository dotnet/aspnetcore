// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

public class CompositeEndpointDataSourceTest
{
    [Fact]
    public void CreatesShallowCopyOf_ListOfEndpoints()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/a");
        var endpoint2 = CreateEndpoint("/b");
        var dataSource = new DefaultEndpointDataSource(new Endpoint[] { endpoint1, endpoint2 });
        var compositeDataSource = new CompositeEndpointDataSource(new[] { dataSource });

        // Act
        var endpoints = compositeDataSource.Endpoints;

        // Assert
        Assert.NotSame(endpoints, dataSource.Endpoints);
        Assert.Equal(endpoints, dataSource.Endpoints);
    }

    [Fact]
    public void CreatesShallowCopyOf_ListOfGroupedEndpoints()
    {
        var endpoint1 = CreateEndpoint("/a");
        var endpoint2 = CreateEndpoint("/b");
        var dataSource = new TestGroupDataSource(new RouteEndpoint[] { endpoint1, endpoint2 });
        var compositeDataSource = new CompositeEndpointDataSource(new[] { dataSource });

        var prefix = RoutePatternFactory.Parse("/");
        var conventions = Array.Empty<Action<EndpointBuilder>>();
        var applicationServices = new ServiceCollection().BuildServiceProvider();

        var groupedEndpoints = compositeDataSource.GetEndpointGroup(new RouteGroupContext(prefix, conventions, applicationServices));

        var resolvedGroupEndpoints = Assert.Single(dataSource.ResolvedGroupedEndpoints);
        Assert.NotSame(groupedEndpoints, resolvedGroupEndpoints);
        Assert.Equal(groupedEndpoints, resolvedGroupEndpoints);
    }

    [Fact]
    public void Endpoints_ReturnsAllEndpoints_FromMultipleDataSources()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/a");
        var endpoint2 = CreateEndpoint("/b");
        var endpoint3 = CreateEndpoint("/c");
        var endpoint4 = CreateEndpoint("/d");
        var endpoint5 = CreateEndpoint("/e");
        var compositeDataSource = new CompositeEndpointDataSource(new[]
        {
                new DefaultEndpointDataSource(new Endpoint[] { endpoint1, endpoint2 }),
                new DefaultEndpointDataSource(new Endpoint[] { endpoint3, endpoint4 }),
                new DefaultEndpointDataSource(new Endpoint[] { endpoint5 }),
        });

        // Act
        var endpoints = compositeDataSource.Endpoints;

        // Assert
        Assert.Collection(
            endpoints,
            (ep) => Assert.Same(endpoint1, ep),
            (ep) => Assert.Same(endpoint2, ep),
            (ep) => Assert.Same(endpoint3, ep),
            (ep) => Assert.Same(endpoint4, ep),
            (ep) => Assert.Same(endpoint5, ep));
    }

    [Fact]
    public void DataSourceChanges_AreReflected_InEndpoints()
    {
        // Arrange1
        var endpoint1 = CreateEndpoint("/a");
        var dataSource1 = new DynamicEndpointDataSource(endpoint1);
        var compositeDataSource = new CompositeEndpointDataSource(new[] { dataSource1 });

        // Act1
        var endpoints = compositeDataSource.Endpoints;

        // Assert1
        var endpoint = Assert.Single(endpoints);
        Assert.Same(endpoint1, endpoint);

        // Arrange2
        var endpoint2 = CreateEndpoint("/b");

        // Act2
        dataSource1.AddEndpoint(endpoint2);

        // Assert2
        Assert.Collection(
            compositeDataSource.Endpoints,
            (ep) => Assert.Same(endpoint1, ep),
            (ep) => Assert.Same(endpoint2, ep));

        // Arrange3
        var endpoint3 = CreateEndpoint("/c");

        // Act2
        dataSource1.AddEndpoint(endpoint3);

        // Assert2
        Assert.Collection(
            compositeDataSource.Endpoints,
            (ep) => Assert.Same(endpoint1, ep),
            (ep) => Assert.Same(endpoint2, ep),
            (ep) => Assert.Same(endpoint3, ep));
    }

    [Fact]
    public void ConsumerChangeToken_IsRefreshed_WhenDataSourceCallbackFires()
    {
        // Arrange1
        var endpoint1 = CreateEndpoint("/a");
        var dataSource1 = new DynamicEndpointDataSource(endpoint1);
        var compositeDataSource = new CompositeEndpointDataSource(new[] { dataSource1 });

        // Act1
        var endpoints = compositeDataSource.Endpoints;

        // Assert1
        var changeToken1 = compositeDataSource.GetChangeToken();
        var token = Assert.IsType<CancellationChangeToken>(changeToken1);
        Assert.False(token.HasChanged); // initial state

        // Arrange2
        var endpoint2 = CreateEndpoint("/b");

        // Act2
        dataSource1.AddEndpoint(endpoint2);

        // Assert2
        Assert.True(changeToken1.HasChanged); // old token is expected to be changed
        var changeToken2 = compositeDataSource.GetChangeToken(); // new token is in a unchanged state
        Assert.NotSame(changeToken2, changeToken1);
        token = Assert.IsType<CancellationChangeToken>(changeToken2);
        Assert.False(token.HasChanged);

        // Arrange3
        var endpoint3 = CreateEndpoint("/c");

        // Act2
        dataSource1.AddEndpoint(endpoint3);

        // Assert2
        Assert.True(changeToken2.HasChanged); // old token is expected to be changed
        var changeToken3 = compositeDataSource.GetChangeToken(); // new token is in a unchanged state
        Assert.NotSame(changeToken3, changeToken2);
        Assert.NotSame(changeToken3, changeToken1);
        token = Assert.IsType<CancellationChangeToken>(changeToken3);
        Assert.False(token.HasChanged);
    }

    [Fact]
    public void ConsumerChangeToken_IsRefreshed_WhenNewDataSourceCallbackFires()
    {
        var endpoint1 = CreateEndpoint("/a");
        var dataSource1 = new DynamicEndpointDataSource(endpoint1);
        var observableCollection = new ObservableCollection<EndpointDataSource> { dataSource1 };
        var compositeDataSource = new CompositeEndpointDataSource(observableCollection);

        var changeToken1 = compositeDataSource.GetChangeToken();
        var token = Assert.IsType<CancellationChangeToken>(changeToken1);
        Assert.False(token.HasChanged);

        var endpoint2 = CreateEndpoint("/b");

        // Update ObservableCollection with a new DynamicEndpointDataSource 
        var dataSource2 = new DynamicEndpointDataSource(endpoint2);
        observableCollection.Add(dataSource2);

        Assert.True(changeToken1.HasChanged);
        var changeToken2 = compositeDataSource.GetChangeToken();
        Assert.NotSame(changeToken2, changeToken1);
        token = Assert.IsType<CancellationChangeToken>(changeToken2);
        Assert.False(token.HasChanged);

        // Update the newly added DynamicEndpointDataSource 
        var endpoint3 = CreateEndpoint("/c");
        dataSource2.AddEndpoint(endpoint3);

        Assert.True(changeToken2.HasChanged);
        var changeToken3 = compositeDataSource.GetChangeToken();
        Assert.NotSame(changeToken3, changeToken2);
        Assert.NotSame(changeToken3, changeToken1);
        token = Assert.IsType<CancellationChangeToken>(changeToken3);
        Assert.False(token.HasChanged);
    }

    [Fact]
    public void ConsumerChangeToken_IsNotRefreshed_AfterDisposal()
    {
        var endpoint1 = CreateEndpoint("/a");
        var dataSource1 = new DynamicEndpointDataSource(endpoint1);
        var observableCollection = new ObservableCollection<EndpointDataSource> { dataSource1 };
        var compositeDataSource = new CompositeEndpointDataSource(observableCollection);

        var changeToken1 = compositeDataSource.GetChangeToken();
        var token = Assert.IsType<CancellationChangeToken>(changeToken1);
        Assert.False(token.HasChanged);

        var endpoint2 = CreateEndpoint("/b");

        // Update DynamicEndpointDatasource
        dataSource1.AddEndpoint(endpoint2);

        Assert.True(changeToken1.HasChanged);
        var changeToken2 = compositeDataSource.GetChangeToken();
        Assert.NotSame(changeToken2, changeToken1);
        token = Assert.IsType<CancellationChangeToken>(changeToken2);
        Assert.False(token.HasChanged);

        // Update ObservableCollection
        var endpoint3 = CreateEndpoint("/c");
        var datasource2 = new DynamicEndpointDataSource(endpoint3);
        observableCollection.Add(datasource2);

        Assert.True(changeToken2.HasChanged);
        var changeToken3 = compositeDataSource.GetChangeToken();
        Assert.NotSame(changeToken3, changeToken2);
        Assert.NotSame(changeToken3, changeToken1);
        token = Assert.IsType<CancellationChangeToken>(changeToken3);
        Assert.False(token.HasChanged);

        compositeDataSource.Dispose();

        // Update DynamicEndpointDatasource and ObservableCollection after disposing CompositeEndpointDataSource.
        var endpoint4 = CreateEndpoint("/d");
        dataSource1.AddEndpoint(endpoint4);
        var endpoint5 = CreateEndpoint("/d");
        var datasource3 = new DynamicEndpointDataSource(endpoint5);
        observableCollection.Add(datasource3);

        // Token is not changed since the CompositeEndpointDataSource was disposed prior to the last endpoint being added.
        Assert.False(changeToken3.HasChanged);
    }

    [Fact]
    public void GetGroupedEndpoints_ForwardedToChildDataSources()
    {
        var endpoint = CreateEndpoint("/a");
        var dataSource = new TestGroupDataSource(new RouteEndpoint[] { endpoint });
        var compositeDataSource = new CompositeEndpointDataSource(new[] { dataSource });

        var prefix = RoutePatternFactory.Parse("/prefix");
        var applicationServices = new ServiceCollection().BuildServiceProvider();
        var metadata = new EndpointNameMetadata("name");
        var conventions = new Action<EndpointBuilder>[]
        {
            b => b.Metadata.Add(metadata),
        };

        var context = new RouteGroupContext(prefix, conventions, applicationServices);
        var groupedEndpoints = compositeDataSource.GetEndpointGroup(context);

        var receivedContext = Assert.Single(dataSource.ReceivedRouteGroupContexts);
        Assert.Same(context, receivedContext);

        var resolvedEndpoint = Assert.IsType<RouteEndpoint>(Assert.Single(groupedEndpoints));
        Assert.Equal("/prefix/a", resolvedEndpoint.RoutePattern.RawText);
        var resolvedMetadata = Assert.Single(resolvedEndpoint.Metadata);
        Assert.Same(metadata, resolvedMetadata);
    }

    private RouteEndpoint CreateEndpoint(
        string template,
        object defaults = null,
        int order = 0,
        string routeName = null)
    {
        return new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse(template, defaults, parameterPolicies: null),
            order,
            EndpointMetadataCollection.Empty,
            null);
    }

    private class TestGroupDataSource : EndpointDataSource
    {
        public TestGroupDataSource(params Endpoint[] endpoints) => Endpoints = endpoints;

        public override IReadOnlyList<Endpoint> Endpoints { get; }

        public List<RouteGroupContext> ReceivedRouteGroupContexts { get; } = new();

        public List<IReadOnlyList<Endpoint>> ResolvedGroupedEndpoints { get; } = new();

        public override IReadOnlyList<Endpoint> GetEndpointGroup(RouteGroupContext context)
        {
            ReceivedRouteGroupContexts.Add(context);
            var resolved = base.GetEndpointGroup(context);
            ResolvedGroupedEndpoints.Add(resolved);
            return resolved;
        }

        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;
    }
}
