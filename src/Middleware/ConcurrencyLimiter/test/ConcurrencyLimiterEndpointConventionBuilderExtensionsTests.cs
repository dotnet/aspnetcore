// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests
{
    public class ConcurrencyLimiterEndpointConventionBuilderExtensionsTests
    {
        [Fact]
        public void RequireStackPolicy_StackPolicyAttribute()
        {
            // Arrange
            var builder = new TestEndpointConventionBuilder();

            // Act
            builder.RequireStackPolicy(maxConcurrentRequests: 1, requestQueueLimit: 1);

            // Assert
            var convention = Assert.Single(builder.Conventions);
            var endpoint = new RouteEndpointBuilder(context => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            convention(endpoint);

            Assert.IsAssignableFrom<StackPolicyAttribute>(Assert.Single(endpoint.Metadata));
        }
        [Fact]
        public void RequireQueuePolicy_QueuePolicyAttribute()
        {
            // Arrange
            var builder = new TestEndpointConventionBuilder();

            // Act
            builder.RequireQueuePolicy(maxConcurrentRequests: 1, requestQueueLimit: 1);

            // Assert
            var convention = Assert.Single(builder.Conventions);
            var endpoint = new RouteEndpointBuilder(context => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            convention(endpoint);

            Assert.IsAssignableFrom<QueuePolicyAttribute>(Assert.Single(endpoint.Metadata));
        }
        [Fact]
        public void SupressQueuePolicy_ISuppressQueuePolicyMetadata()
        {
            // Arrange
            var builder = new TestEndpointConventionBuilder();

            // Act
            builder.SupressQueuePolicy();

            // Assert
            var convention = Assert.Single(builder.Conventions);
            var endpoint = new RouteEndpointBuilder(context => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            convention(endpoint);

            Assert.IsAssignableFrom<ISuppressQueuePolicyMetadata>(Assert.Single(endpoint.Metadata));
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
}
