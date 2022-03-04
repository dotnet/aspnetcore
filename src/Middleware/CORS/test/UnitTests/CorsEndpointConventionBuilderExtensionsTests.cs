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
        public void RequireCors_Name_MetadataAdded()
        {
            // Arrange
            var testConventionBuilder = new TestEndpointConventionBuilder();

            // Act
            testConventionBuilder.RequireCors("TestPolicyName");

            // Assert
            var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

            var endpointModel = new TestEndpointBuilder();
            addCorsPolicy(endpointModel);
            var endpoint = endpointModel.Build();

            var metadata = endpoint.Metadata.GetMetadata<IEnableCorsAttribute>();
            Assert.NotNull(metadata);
            Assert.Equal("TestPolicyName", metadata.PolicyName);
        }

        [Fact]
        public void RequireCors_Policy_MetadataAdded()
        {
            // Arrange
            var testConventionBuilder = new TestEndpointConventionBuilder();

            // Act
            testConventionBuilder.RequireCors(builder => builder.AllowAnyOrigin());

            // Assert
            var addCorsPolicy = Assert.Single(testConventionBuilder.Conventions);

            var endpointBuilder = new TestEndpointBuilder();
            addCorsPolicy(endpointBuilder);
            var endpoint = endpointBuilder.Build();

            var metadata = endpoint.Metadata.GetMetadata<ICorsPolicyMetadata>();
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Policy);
            Assert.True(metadata.Policy.AllowAnyOrigin);
        }

        [Fact]
        public void RequireCors_ChainedCall_ReturnedBuilderIsDerivedType()
        {
            // Arrange
            var testConventionBuilder = new TestEndpointConventionBuilder();

            // Act
            var builder = testConventionBuilder.RequireCors("TestPolicyName");

            // Assert
            Assert.True(builder.TestProperty);
        }

        private class TestEndpointBuilder : EndpointBuilder
        {
            public override Endpoint Build()
            {
                return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
            }
        }

        private class TestEndpointConventionBuilder : IEndpointConventionBuilder
        {
            public IList<Action<EndpointBuilder>> Conventions { get; } = new List<Action<EndpointBuilder>>();
            public bool TestProperty { get; } = true;

            public void Add(Action<EndpointBuilder> convention)
            {
                Conventions.Add(convention);
            }
        }
    }
}
