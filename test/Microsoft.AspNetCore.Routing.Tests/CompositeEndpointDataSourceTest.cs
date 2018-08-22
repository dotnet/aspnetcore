// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
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

        private class CustomEndpointDataSource : EndpointDataSource
        {
            private readonly CancellationTokenSource _cts;
            private readonly CancellationChangeToken _token;

            public CustomEndpointDataSource()
            {
                _cts = new CancellationTokenSource();
                _token = new CancellationChangeToken(_cts.Token);
            }

            public override IChangeToken GetChangeToken() => _token;
            public override IReadOnlyList<Endpoint> Endpoints => Array.Empty<Endpoint>();
        }
    }
}