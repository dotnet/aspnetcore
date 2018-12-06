// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public class CorsEndpointConventionBuilderExtensionsTests
    {
        [Fact]
        public void WithCorsPolicy_Name_MetadataAdded()
        {
            // Arrange
            var testConventionBuilder = new TestEndpointConventionBuilder();

            // Act
            testConventionBuilder.WithCorsPolicy("TestPolicyName");

            // Assert
            var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

            var endpointModel = new TestEndpointModel();
            addCorsPolicy(endpointModel);
            var endpoint = endpointModel.Build();

            var metadata = endpoint.Metadata.GetMetadata<IEnableCorsAttribute>();
            Assert.NotNull(metadata);
            Assert.Equal("TestPolicyName", metadata.PolicyName);
        }

        [Fact]
        public void WithCorsPolicy_Policy_MetadataAdded()
        {
            // Arrange
            var testConventionBuilder = new TestEndpointConventionBuilder();

            // Act
            testConventionBuilder.WithCorsPolicy(builder => builder.AllowAnyOrigin());

            // Assert
            var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

            var endpointModel = new TestEndpointModel();
            addCorsPolicy(endpointModel);
            var endpoint = endpointModel.Build();

            var metadata = endpoint.Metadata.GetMetadata<ICorsPolicyMetadata>();
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Policy);
            Assert.True(metadata.Policy.AllowAnyOrigin);
        }

        private class TestEndpointModel : EndpointModel
        {
            public override Endpoint Build()
            {
                return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
            }
        }

        private class TestEndpointConventionBuilder : IEndpointConventionBuilder
        {
            public IList<Action<EndpointModel>> Conventions { get; } = new List<Action<EndpointModel>>();

            public void Apply(Action<EndpointModel> convention)
            {
                Conventions.Add(convention);
            }
        }
    }
}
