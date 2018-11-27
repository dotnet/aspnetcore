// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RouteDataTest : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public RouteDataTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task RouteData_Routers_ConventionalRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Routing/Conventional");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(
                new string[]
                {
                    typeof(RouteCollection).FullName,
                    typeof(Route).FullName,
                    typeof(MvcRouteHandler).FullName,
                },
                result.Routers);
        }

        [Fact]
        public async Task RouteData_Routers_AttributeRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Routing/Attribute");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(new string[]
                {
                    typeof(RouteCollection).FullName,
                    typeof(AttributeRoute).FullName,
                    typeof(MvcAttributeRouteHandler).FullName,
                },
                result.Routers);
        }

        // Verifies that components in the MVC pipeline can modify datatokens
        // without impacting any static data.
        //
        // This does two request, to verify that the data in the route is not modified
        [Fact]
        public async Task RouteData_DataTokens_FilterCanSetDataTokens()
        {
            // Arrange
            var response = await Client.GetAsync("http://localhost/Routing/DataTokens");

            // Guard
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);
            Assert.Single(result.DataTokens);
            Assert.Single(result.DataTokens, kvp => kvp.Key == "actionName" && ((string)kvp.Value) == "DataTokens");

            // Act
            response = await Client.GetAsync("http://localhost/Routing/Conventional");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            body = await response.Content.ReadAsStringAsync();
            result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Single(result.DataTokens);
            Assert.Single(result.DataTokens, kvp => kvp.Key == "actionName" && ((string)kvp.Value) == "Conventional");
        }

        private class ResultData
        {
            public Dictionary<string, object> DataTokens { get; set; }

            public string[] Routers { get; set; }
        }
    }
}