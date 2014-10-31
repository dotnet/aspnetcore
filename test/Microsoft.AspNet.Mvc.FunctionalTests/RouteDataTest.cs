// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RouteDataTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(BasicWebSite));
        private readonly Action<IApplicationBuilder> _app = new BasicWebSite.Startup().Configure;

        [Fact]
        public async Task RouteData_Routers_ConventionalRoute()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
                    typeof(TemplateRoute).FullName,
                    typeof(MvcRouteHandler).FullName,
                },
                result.Routers);
        }


        private class ResultData
        {
            public string[] Routers { get; set; }
        }
    }
}