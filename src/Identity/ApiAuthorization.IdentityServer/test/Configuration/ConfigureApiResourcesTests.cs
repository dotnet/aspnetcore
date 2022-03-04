// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    public class ConfigureApiResourcesTests
    {
        [Fact]
        public void GetApiResources_ReadsApisFromConfiguration()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MyAPI:Profile"] = "API"
            }).Build();
            var localApiDescriptor = new TestLocalApiDescriptor();
            var configurationLoader = new ConfigureApiResources(
                configuration,
                localApiDescriptor,
                new TestLogger<ConfigureApiResources>());

            // Act
            var resources = configurationLoader.GetApiResources();

            // Assert
            var resource = Assert.Single(resources);
            var scope = Assert.Single(resource.Scopes);
            Assert.Equal("MyAPI", resource.Name);
            Assert.Equal("MyAPI", scope.Name);
        }

        [Fact]
        public void GetApiResources_ReadsApiScopesFromConfiguration()
        {
            // Arrange
            var expectedScopes = new[] { "First", "Second", "Third" };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MyAPI:Profile"] = "API",
                ["MyAPI:Scopes"] = "First Second Third"
            }).Build();
            var localApiDescriptor = new TestLocalApiDescriptor();
            var configurationLoader = new ConfigureApiResources(
                configuration,
                localApiDescriptor,
                new TestLogger<ConfigureApiResources>());
            // Act
            var resources = configurationLoader.GetApiResources();

            // Assert
            var resource = Assert.Single(resources);
            Assert.Equal("MyAPI", resource.Name);
            Assert.NotNull(resource.Scopes);
            Assert.Equal(3, resource.Scopes.Count);
            Assert.Equal(expectedScopes, resource.Scopes.Select(s => s.Name).ToArray());
        }

        [Fact]
        public void GetApiResources_DetectsLocallyRegisteredApis()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var localApiDescriptor = new TestLocalApiDescriptor(new Dictionary<string, ResourceDefinition>
            {
                ["MyAPI"] = new ResourceDefinition { Profile = ApplicationProfiles.IdentityServerJwt }
            });
            var configurationLoader = new ConfigureApiResources(
                configuration,
                localApiDescriptor,
                new TestLogger<ConfigureApiResources>());

            // Act
            var resources = configurationLoader.GetApiResources();

            // Assert
            var resource = Assert.Single(resources);
            var scope = Assert.Single(resource.Scopes);
            Assert.Equal("MyAPI", resource.Name);
            Assert.Equal("MyAPI", scope.Name);
        }

        [Fact]
        public void Configure_AddsResourcesToExistingResourceList()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MyAPI:Profile"] = "API"
            }).Build();
            var localApiDescriptor = new TestLocalApiDescriptor();
            var configurationLoader = new ConfigureApiResources(
                configuration,
                localApiDescriptor,
                new TestLogger<ConfigureApiResources>());

            var options = new ApiAuthorizationOptions();

            // Act
            configurationLoader.Configure(options);

            // Assert
            var resource = Assert.Single(options.ApiResources);
            var scope = Assert.Single(resource.Scopes);
            Assert.Equal("MyAPI", resource.Name);
            Assert.Equal("MyAPI", scope.Name);
        }

        private class TestLocalApiDescriptor : IIdentityServerJwtDescriptor
        {
            private readonly IDictionary<string, ResourceDefinition> _definitions;

            public TestLocalApiDescriptor()
                : this(new Dictionary<string, ResourceDefinition>())
            {
            }

            public TestLocalApiDescriptor(IDictionary<string, ResourceDefinition> definitions)
            {
                _definitions = definitions;
            }

            public IDictionary<string, ResourceDefinition> GetResourceDefinitions()
            {
                return _definitions;
            }
        }
    }
}
