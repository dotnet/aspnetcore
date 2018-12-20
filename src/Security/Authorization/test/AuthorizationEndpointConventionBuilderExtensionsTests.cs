// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class AuthorizationEndpointConventionBuilderExtensionsTests
    {
        [Fact]
        public void RequireAuthorization_IAuthorizeData()
        {
            // Arrange
            var builder = new TestEndpointConventionBuilder();
            var metadata = new AuthorizeAttribute();

            // Act
            builder.RequireAuthorization(metadata);

            // Assert
            var convention = Assert.Single(builder.Conventions);

            var endpointModel = new RouteEndpointBuilder((context) => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            convention(endpointModel);

            Assert.Equal(metadata, Assert.Single(endpointModel.Metadata));
        }

        [Fact]
        public void RequireAuthorization_PolicyName()
        {
            // Arrange
            var builder = new TestEndpointConventionBuilder();

            // Act
            builder.RequireAuthorization("policy");

            // Assert
            var convention = Assert.Single(builder.Conventions);

            var endpointModel = new RouteEndpointBuilder((context) => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            convention(endpointModel);

            Assert.Equal("policy", Assert.IsAssignableFrom<IAuthorizeData>(Assert.Single(endpointModel.Metadata)).Policy);
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
