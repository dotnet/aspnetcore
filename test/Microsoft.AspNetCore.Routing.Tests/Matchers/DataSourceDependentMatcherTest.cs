// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.TestObjects;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class DataSourceDependentMatcherTest
    {
        [Fact]
        public void Matcher_Initializes_InConstructor()
        {
            // Arrange
            var dataSource = new DynamicEndpointDataSource();

            // Act
            var matcher = new DataSourceDependentMatcher(dataSource, TestMatcherBuilder.Create);

            // Assert
            var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
            Assert.Empty(inner.Endpoints);
        }

        [Fact]
        public void Matcher_Reinitializes_WhenDataSourceChanges()
        {
            // Arrange
            var dataSource = new DynamicEndpointDataSource();
            var matcher = new DataSourceDependentMatcher(dataSource, TestMatcherBuilder.Create);

            var endpoint = new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                "a/b/c",
                new RouteValueDictionary(),
                new RouteValueDictionary(),
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
        public void Matcher_Ignores_NonMatcherEndpoint()
        {
            // Arrange
            var dataSource = new DynamicEndpointDataSource();
            var endpoint = new TestEndpoint(EndpointMetadataCollection.Empty, "test");
            dataSource.AddEndpoint(endpoint);

            // Act
            var matcher = new DataSourceDependentMatcher(dataSource, TestMatcherBuilder.Create);

            // Assert
            var inner = Assert.IsType<TestMatcher>(matcher.CurrentMatcher);
            Assert.Empty(inner.Endpoints);
        }

        [Fact]
        public void Cache_Reinitializes_WhenDataSourceChanges()
        {
            // Arrange
            var count = 0;

            var dataSource = new DynamicEndpointDataSource();
            var cache = new DataSourceDependentCache<string>(dataSource, (endpoints) =>
            {
                count++;
                return $"hello, {count}!";
            });

            cache.EnsureInitialized();
            Assert.Equal("hello, 1!", cache.Value);

            // Act
            dataSource.AddEndpoint(null);

            // Assert
            Assert.Equal(2, count);
            Assert.Equal("hello, 2!", cache.Value);
        }

        private class TestMatcherBuilder : MatcherBuilder
        {
            public static Func<MatcherBuilder> Create = () => new TestMatcherBuilder();

            private List<MatcherEndpoint> Endpoints { get; } = new List<MatcherEndpoint>();

            public override void AddEndpoint(MatcherEndpoint endpoint)
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
            public IReadOnlyList<MatcherEndpoint> Endpoints { get; set; }

            public override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
            {
                throw new NotImplementedException();
            }
        }
    }
}
