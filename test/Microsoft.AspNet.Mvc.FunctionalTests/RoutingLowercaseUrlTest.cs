// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // This website sets the generation of lowercase URLs to true
    public class RoutingLowercaseUrlTest : IClassFixture<MvcTestFixture<LowercaseUrlsWebSite.Startup>>
    {
        public RoutingLowercaseUrlTest(MvcTestFixture<LowercaseUrlsWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

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
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/" + path);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedUrl, body);
        }
    }
}