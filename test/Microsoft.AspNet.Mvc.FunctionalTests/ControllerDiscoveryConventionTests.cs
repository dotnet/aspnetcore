// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ControllerDiscoveryConventionTests :
        IClassFixture<MvcTestFixture<ControllerDiscoveryConventionsWebSite.Startup>>,
        IClassFixture<FilteredDefaultAssemblyProviderFixture<ControllerDiscoveryConventionsWebSite.Startup>>
    {
        public ControllerDiscoveryConventionTests(
            MvcTestFixture<ControllerDiscoveryConventionsWebSite.Startup> fixture,
            FilteredDefaultAssemblyProviderFixture<ControllerDiscoveryConventionsWebSite.Startup> filteredFixture)
        {
            Client = fixture.Client;
            FilteredClient = filteredFixture.Client;
        }

        public HttpClient Client { get; }

        public HttpClient FilteredClient { get; }

        [Fact]
        public async Task AbstractControllers_AreSkipped()
        {
            // Arrange & Act
            var response = await Client.GetAsync("Abstract/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TypesDerivingFromControllerBaseTypesThatDoNotReferenceMvc_AreSkipped()
        {
            // Arrange & Act
            var response = await Client.GetAsync("SqlTransactionManager/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TypesMarkedWithNonController_AreSkipped()
        {
            // Arrange & Act
            var response = await Client.GetAsync("NonController/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PocoTypesWithControllerSuffix_AreDiscovered()
        {
            // Arrange & Act
            var response = await Client.GetAsync("Poco/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PocoController", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypesDerivingFromTypesWithControllerSuffix_AreDiscovered()
        {
            // Arrange & Act
            var response = await Client.GetAsync("ChildOfAbstract/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("AbstractController", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypesDerivingFromApiController_AreDiscovered()
        {
            // Arrange & Act
            var response = await FilteredClient.GetAsync("PersonApi/GetValue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PersonApi", await response.Content.ReadAsStringAsync());
        }
    }
}
