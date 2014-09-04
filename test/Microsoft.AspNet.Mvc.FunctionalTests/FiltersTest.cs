// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FiltersTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FiltersWebSite");
        private readonly Action<IApplicationBuilder> _app = new FiltersWebSite.Startup().Configure;

        [Fact]
        public async Task ListAllFilters()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Products/GetPrice/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<decimal>(body);

            Assert.Equal(19.95m, result);

            var filters = response.Headers.GetValues("filters");
            Assert.Equal(
                new string[]
                {
                    // This one uses order to set itself 'first' even though it appears on the controller
                    "FiltersWebSite.PassThroughActionFilter",

                    // Configured as global with default order
                    "FiltersWebSite.GlobalExceptionFilter",

                    // Configured on the controller with default order
                    "FiltersWebSite.PassThroughResultFilter",

                    // Configured on the action with default order
                    "FiltersWebSite.PassThroughActionFilter",

                    // The controller itself
                    "FiltersWebSite.ProductsController",
                },
                filters);
        }
    }
}