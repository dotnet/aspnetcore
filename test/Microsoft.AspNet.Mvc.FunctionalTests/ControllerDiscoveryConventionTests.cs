// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ControllerDiscoveryConventionsWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ControllerDiscoveryConventionTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(
            nameof(ControllerDiscoveryConventionsWebSite));
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task AbstractControllers_AreSkipped()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost/");

            // Act
            var response = await client.GetAsync("Abstract/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TypesDerivingFromControllerBaseTypesThatDoNotReferenceMvc_AreSkipped()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost/");

            // Act
            var response = await client.GetAsync("SqlTransactionManager/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TypesMarkedWithNonController_AreSkipped()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost/");

            // Act
            var response = await client.GetAsync("NonController/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PocoTypesWithControllerSuffix_AreDiscovered()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost/");

            // Act
            var response = await client.GetAsync("Poco/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PocoController", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypesDerivingFromTypesWithControllerSuffix_AreDiscovered()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost/");

            // Act
            var response = await client.GetAsync("ChildOfAbstract/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("AbstractController", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypesDerivingFromApiController_AreDiscovered()
        {
            // Arrange
            // TestHelper.CreateServices replaces the DefaultAssemblyProvider with a provider that
            // limits the set of candidate assemblies to the executing application. For this test,
            // we'll switch it back to using a filtered default assembly provider.
            var services = HostingServices.Create(configuration: null);
            services.AddTransient<IAssemblyProvider, FilteredDefaultAssemblyProvider>();
            var serviceProvider = TestHelper.CreateServices(nameof(ControllerDiscoveryConventionsWebSite), services);
            var server = TestServer.Create(serviceProvider, _app);

            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost/");

            // Act
            var response = await client.GetAsync("PersonApi/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PersonApi", await response.Content.ReadAsStringAsync());
        }

        private class FilteredDefaultAssemblyProvider : DefaultAssemblyProvider
        {
            public FilteredDefaultAssemblyProvider(ILibraryManager libraryManager)
                : base(libraryManager)
            {

            }

            protected override IEnumerable<ILibraryInformation> GetCandidateLibraries()
            {
                var libraries = base.GetCandidateLibraries();
                // Filter out other WebSite projects
                return libraries.Where(library => !library.Name.Contains("WebSite") ||
                        library.Name.Equals(nameof(ControllerDiscoveryConventionsWebSite), StringComparison.Ordinal));
            }
        }
    }
}
