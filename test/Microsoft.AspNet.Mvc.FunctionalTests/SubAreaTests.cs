// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class SubAreaTests : IClassFixture<MvcTestFixture<MvcSubAreaSample.Web.Startup>>
    {
        public SubAreaTests(MvcTestFixture<MvcSubAreaSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task SubArea_Menu()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/Restaurant/Menu/Home/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Item 1", content);
        }

        [Fact]
        public async Task SubArea_Hours()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/Restaurant/Hours/Home/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("9-5 M-F", content);
        }

        [Fact]
        public async Task SubArea_Home()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Areas", content);
        }
    }
}
