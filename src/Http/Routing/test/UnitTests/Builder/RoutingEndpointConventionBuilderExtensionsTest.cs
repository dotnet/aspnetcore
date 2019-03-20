// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Builder
{
    public class RoutingEndpointConventionBuilderExtensionsTest
    {
        [Fact]
        public void RequireHost_AddsHostMetadata()
        {
            // Arrange
            var builder = CreateBuilder();

            // Act
            builder.RequireHost("www.example.com", "example.com");

            // Assert
            var endpoint = builder.Build();

            var metadata = endpoint.Metadata.GetMetadata<IHostMetadata>();
            Assert.NotNull(metadata);
            Assert.Equal(new[] { "www.example.com", "example.com" }, metadata.Hosts);
        }

        [Fact]
        public void WithDisplayName_String_SetsDisplayName()
        {
            // Arrange
            var builder = CreateBuilder();

            // Act
            builder.WithDisplayName("test");

            // Assert
            var endpoint = builder.Build();
            Assert.Equal("test", endpoint.DisplayName);
        }

        [Fact]
        public void WithDisplayName_Func_SetsDisplayName()
        {
            // Arrange
            var builder = CreateBuilder();

            // Act
            builder.WithDisplayName(b => "test");

            // Assert
            var endpoint = builder.Build();
            Assert.Equal("test", endpoint.DisplayName);
        }

        [Fact]
        public void WithMetadata_AddsMetadata()
        {
            // Arrange
            var builder = CreateBuilder();

            // Act
            builder.WithMetadata("test", new HostAttribute("www.example.com", "example.com"));

            // Assert
            var endpoint = builder.Build();

            var hosts = endpoint.Metadata.GetMetadata<IHostMetadata>();
            Assert.NotNull(hosts);
            Assert.Equal(new[] { "www.example.com", "example.com" }, hosts.Hosts);

            var @string = endpoint.Metadata.GetMetadata<string>();
            Assert.Equal("test", @string);
        }

        private DefaultEndpointConventionBuilder CreateBuilder()
        {
            return new DefaultEndpointConventionBuilder(new RouteEndpointBuilder(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse("/test"),
                order: 0));
        }
    }
}
