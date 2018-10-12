// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Internal
{
    public class DfaGraphWriterTest
    {
        private DfaGraphWriter CreateGraphWriter()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddRouting();

            return new DfaGraphWriter(services.BuildServiceProvider());
        }

        [Fact]
        public void Write_ExcludeNonRouteEndpoint()
        {
            // Arrange
            var graphWriter = CreateGraphWriter();
            var writer = new StringWriter();
            var endpointsDataSource = new DefaultEndpointDataSource(new Endpoint((context) => null, EndpointMetadataCollection.Empty, string.Empty));

            // Act
            graphWriter.Write(endpointsDataSource, writer);

            // Assert
            Assert.Equal(@"digraph DFA {
0 [label=""/""]
}
", writer.ToString());
        }

        [Fact]
        public void Write_ExcludeRouteEndpointWithSuppressMatchingMetadata()
        {
            // Arrange
            var graphWriter = CreateGraphWriter();
            var writer = new StringWriter();
            var endpointsDataSource = new DefaultEndpointDataSource(
                new RouteEndpoint(
                    (context) => null,
                    RoutePatternFactory.Parse("/"),
                    0,
                    new EndpointMetadataCollection(new SuppressMatchingMetadata()),
                    string.Empty));

            // Act
            graphWriter.Write(endpointsDataSource, writer);

            // Assert
            Assert.Equal(@"digraph DFA {
0 [label=""/""]
}
", writer.ToString());
        }

        [Fact]
        public void Write_IncludeRouteEndpointWithPolicy()
        {
            // Arrange
            var graphWriter = CreateGraphWriter();
            var writer = new StringWriter();
            var endpointsDataSource = new DefaultEndpointDataSource(
                new RouteEndpoint(
                    (context) => null,
                    RoutePatternFactory.Parse("/"),
                    0,
                    new EndpointMetadataCollection(new HttpMethodMetadata(new[] { "GET" })),
                    string.Empty));

            // Act
            graphWriter.Write(endpointsDataSource, writer);

            // Assert
            var sdf = writer.ToString();
            Assert.Equal(@"digraph DFA {
0 [label=""/ HTTP: GET""]
1 [label=""/ HTTP: *""]
2 -> 0 [label=""HTTP: GET""]
2 -> 1 [label=""HTTP: *""]
2 [label=""/""]
}
", sdf);
        }
    }
}
