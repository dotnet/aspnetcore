// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using MvcTagHelpersWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcTagHelpersTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("MvcTagHelpersWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private static readonly Assembly _resourcesAssembly = typeof(TagHelpersTests).GetTypeInfo().Assembly;

        [Theory]
        [InlineData("Index", null)]
        [InlineData("Order", "/MvcTagHelper_Order/Submit")]
        [InlineData("Product", null)]
        [InlineData("Customer", "/Customer/MvcTagHelper_Customer")]
        // Testing InputTagHelpers invoked in the partial views 
        [InlineData("ProductList", null)] 
        // Testing MvcTagHelpers invoked in the editor templates with the HTML helpers
        [InlineData("EmployeeList", null)] 
        // Testing SelectTagHelper with Html.BeginForm 
        [InlineData("CreateWarehouse", null)] 
        // Testing the HTML helpers with FormTagHelper
        [InlineData("EditWarehouse", null)] 
        public async Task MvcTagHelpers_GeneratesExpectedResults(string action, string antiForgeryPath)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

            // The K runtime compiles every file under compiler/resources as a resource at runtime with the same name
            // as the file name, in order to update a baseline you just need to change the file in that folder.
            var expectedContent =
                    await _resourcesAssembly.ReadResourceAsStringAsync
                                     ("compiler/resources/MvcTagHelpersWebSite.MvcTagHelper_Home." + action + ".html");

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/MvcTagHelper_Home/" + action);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            if (antiForgeryPath != null)
            {
                var forgeryToken = AntiForgeryTestHelper.RetrieveAntiForgeryToken(responseContent, antiForgeryPath);
                expectedContent = string.Format(expectedContent, forgeryToken);
            }
            Assert.Equal(expectedContent.Trim(), responseContent.Trim());
        }

        [Fact]
        public async Task ValidationTagHelpers_GeneratesExpectedSpansAndDivs()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedContent =
                    await _resourcesAssembly.ReadResourceAsStringAsync
                                     ("compiler/resources/MvcTagHelpersWebSite.MvcTagHelper_Customer.Index.html");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Customer/MvcTagHelper_Customer");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Number", string.Empty),
                new KeyValuePair<string,string>("Name", string.Empty),
                new KeyValuePair<string,string>("Email", string.Empty),
                new KeyValuePair<string,string>("PhoneNumber", string.Empty),
                new KeyValuePair<string,string>("Password", string.Empty)
            };
            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var forgeryToken = AntiForgeryTestHelper.RetrieveAntiForgeryToken(responseContent, "Customer/MvcTagHelper_Customer");
            expectedContent = string.Format(expectedContent, forgeryToken);
            Assert.Equal(expectedContent.Trim(), responseContent.Trim());
        }
    }
}