// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RoutingLowercaseUrlTest
    {
        private const string SiteName = nameof(LowercaseUrlsWebSite);

        // This website sets the generation of lowercase URLs to true
        private readonly Action<IApplicationBuilder> _app = new LowercaseUrlsWebSite.Startup().Configure;

        [Theory]
        // Generating lower case URL doesnt lowercase the query parameters
        [InlineData("LowercaseUrls_Blog/Generatelink", "/api/employee/getemployee/marykae?LastName=McDonald")]

        // Lowercasing controller and action for conventional route
        [InlineData("LowercaseUrls_Blog/ShowPosts", "/lowercaseurls_blog/showposts")]

        // Lowercasing controller, action and other route data for conventional route
        [InlineData("LowercaseUrls_Blog/edit/BlogPost", "/lowercaseurls_blog/edit/blogpost")]

        // Lowercasing controller and action for attribute route
        [InlineData("Api/Employee/List", "/api/employee/list")]

        // Lowercasing controller, action and other route data for attribute route
        [InlineData("Api/Employee/GetEmployee/JohnDoe", "/api/employee/getemployee/johndoe")]
        public async Task GenerateLowerCaseUrlsTests(string path, string expectedUrl)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/" + path);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedUrl, body);
        }
    }
}