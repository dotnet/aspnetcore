// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RouteDataTest
    {
        private const string SiteName = nameof(BasicWebSite);
        private readonly Action<IApplicationBuilder> _app = new BasicWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new BasicWebSite.Startup().ConfigureServices;

        [Fact]
        public async Task RouteData_Routers_ConventionalRoute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Routing/Conventional");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(new string[]
                {
                    typeof(RouteCollection).FullName,
                    typeof(TemplateRoute).FullName,
                    typeof(MvcRouteHandler).FullName,
                },
                result.Routers);
        }

        [Fact]
        public async Task RouteData_Routers_AttributeRoute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Routing/Attribute");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(new string[]
                {
                    typeof(RouteCollection).FullName,
                    typeof(AttributeRoute).FullName,
                    typeof(MvcRouteHandler).FullName,
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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var response = await client.GetAsync("http://localhost/Routing/DataTokens");

            // Guard
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);
            Assert.Single(result.DataTokens);
            Assert.Single(result.DataTokens, kvp => kvp.Key == "actionName" && ((string)kvp.Value) == "DataTokens");

            // Act
            response = await client.GetAsync("http://localhost/Routing/Conventional");

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