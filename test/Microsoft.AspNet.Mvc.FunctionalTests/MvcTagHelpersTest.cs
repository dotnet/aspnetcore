// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using MvcTagHelpersWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcTagHelpersTest
    {
        private const string SiteName = nameof(MvcTagHelpersWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private static readonly Assembly _resourcesAssembly = typeof(MvcTagHelpersTest).GetTypeInfo().Assembly;

        [Theory]
        [InlineData("Index", null)]
        // Test ability to generate nearly identical HTML with MVC tag and HTML helpers.
        // Only attribute order should differ.
        [InlineData("Order", "/MvcTagHelper_Order/Submit")]
        [InlineData("OrderUsingHtmlHelpers", "/MvcTagHelper_Order/Submit")]
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
        // Testing the EnvironmentTagHelper
        [InlineData("Environment", null)]
        // Testing the LinkTagHelper
        [InlineData("Link", null)]
        // Testing the ScriptTagHelper
        [InlineData("Script", null)]
        public async Task MvcTagHelpers_GeneratesExpectedResults(string action, string antiForgeryPath)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

            // The K runtime compiles every file under compiler/resources as a resource at runtime with the same name
            // as the file name, in order to update a baseline you just need to change the file in that folder.
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync(
                "compiler/resources/MvcTagHelpersWebSite.MvcTagHelper_Home." + action + ".html");

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync(
                "compiler/resources/MvcTagHelpersWebSite.MvcTagHelper_Customer.Index.html");

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

        [Fact]
        public async Task CacheTagHelper_CanCachePortionsOfViewsPartialViewsAndViewComponents()
        {
            // Arrange
            var assertFile =
                "compiler/resources/CacheTagHelper_CanCachePortionsOfViewsPartialViewsAndViewComponents.Assert";
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");
            client.DefaultRequestHeaders.Add("Locale", "North");

            // Act - 1
            // Verify that content gets cached based on vary-by-params
            var targetUrl = "/catalog?categoryId=1&correlationid=1";
            var response1 = await client.GetStringAsync(targetUrl);
            var response2 = await client.GetStringAsync(targetUrl);

            // Assert - 1
            var expected1 = await _resourcesAssembly.ReadResourceAsStringAsync(assertFile + "1.txt");

            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected1, response2.Trim());

            // Act - 2
            // Verify content gets changed in partials when one of the vary by parameters is changed
            targetUrl = "/catalog?categoryId=3&correlationid=2";
            var response3 = await client.GetStringAsync(targetUrl);
            var response4 = await client.GetStringAsync(targetUrl);

            // Assert - 2
            var expected2 = await _resourcesAssembly.ReadResourceAsStringAsync(assertFile + "2.txt");

            Assert.Equal(expected2, response3.Trim());
            Assert.Equal(expected2, response4.Trim());

            // Act - 3
            // Verify content gets changed in a View Component when the Vary-by-header parameters is changed
            client.DefaultRequestHeaders.Remove("Locale");
            client.DefaultRequestHeaders.Add("Locale", "East");

            targetUrl = "/catalog?categoryId=3&correlationid=3";
            var response5 = await client.GetStringAsync(targetUrl);
            var response6 = await client.GetStringAsync(targetUrl);

            // Assert - 3
            var expected3 = await _resourcesAssembly.ReadResourceAsStringAsync(assertFile + "3.txt");

            Assert.Equal(expected3, response5.Trim());
            Assert.Equal(expected3, response6.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_ExpiresContent_BasedOnExpiresParameter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/catalog/2");

            // Assert - 1
            var expected1 = "Cached content for 2";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            await Task.Delay(TimeSpan.FromSeconds(1));
            var response2 = await client.GetStringAsync("/catalog/3");

            // Assert - 2
            var expected2 = "Cached content for 3";
            Assert.Equal(expected2, response2.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_UsesVaryByCookie_ToVaryContent()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/catalog/cart?correlationid=1");

            // Assert - 1
            var expected1 = "Cart content for 1";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            client.DefaultRequestHeaders.Add("Cookie", "CartId=10");
            var response2 = await client.GetStringAsync("/catalog/cart?correlationid=2");

            // Assert - 2
            var expected2 = "Cart content for 2";
            Assert.Equal(expected2, response2.Trim());

            // Act - 3
            // Resend the cookiesless request and cached result from the first response.
            client.DefaultRequestHeaders.Remove("Cookie");
            var response3 = await client.GetStringAsync("/catalog/cart?correlationid=3");

            // Assert - 3
            Assert.Equal(expected1, response3.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_VariesByRoute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync(
                "/catalog/north-west/confirm-payment?confirmationId=1");

            // Assert - 1
            var expected1 = "Welcome Guest. Your confirmation id is 1. (Region north-west)";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            var response2 = await client.GetStringAsync(
                "/catalog/south-central/confirm-payment?confirmationId=2");

            // Assert - 2
            var expected2 = "Welcome Guest. Your confirmation id is 2. (Region south-central)";
            Assert.Equal(expected2, response2.Trim());

            // Act 3
            var response3 = await client.GetStringAsync(
                "/catalog/north-west/Silver/confirm-payment?confirmationId=4");

            var expected3 = "Welcome Silver member. Your confirmation id is 4. (Region north-west)";
            Assert.Equal(expected3, response3.Trim());

            // Act 4
            var response4 = await client.GetStringAsync(
                "/catalog/north-west/Gold/confirm-payment?confirmationId=5");

            var expected4 = "Welcome Gold member. Your confirmation id is 5. (Region north-west)";
            Assert.Equal(expected4, response4.Trim());

            // Act - 4
            // Resend the responses and expect cached results.
            response1 = await client.GetStringAsync(
                "/catalog/north-west/confirm-payment?confirmationId=301");
            response2 = await client.GetStringAsync(
                "/catalog/south-central/confirm-payment?confirmationId=402");
            response3 = await client.GetStringAsync(
                "/catalog/north-west/Silver/confirm-payment?confirmationId=503");
            response4 = await client.GetStringAsync(
                "/catalog/north-west/Gold/confirm-payment?confirmationId=608");

            // Assert - 4
            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected2, response2.Trim());
            Assert.Equal(expected3, response3.Trim());
            Assert.Equal(expected4, response4.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_VariesByUserId()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/catalog/past-purchases/test1?correlationid=1");
            var response2 = await client.GetStringAsync("/catalog/past-purchases/test1?correlationid=2");

            // Assert - 1
            var expected1 = "Past purchases for user test1 (1)";
            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected1, response2.Trim());

            // Act - 2
            var response3 = await client.GetStringAsync("/catalog/past-purchases/test2?correlationid=3");
            var response4 = await client.GetStringAsync("/catalog/past-purchases/test2?correlationid=4");

            // Assert - 2
            var expected2 = "Past purchases for user test2 (3)";
            Assert.Equal(expected2, response3.Trim());
            Assert.Equal(expected2, response4.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_BubblesExpirationOfNestedTagHelpers()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/categories/Books?correlationId=1");

            // Assert - 1
            var expected1 =
@"Category: Books
Products: Book1, Book2 (1)";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            var response2 = await client.GetStringAsync("/categories/Electronics?correlationId=2");

            // Assert - 2
            var expected2 =
@"Category: Electronics
Products: Book1, Book2 (1)";
            Assert.Equal(expected2, response2.Trim());

            // Act - 3
            // Trigger an expiration
            var response3 = await client.PostAsync("/categories/update-products", new StringContent(string.Empty));
            response3.EnsureSuccessStatusCode();

            var response4 = await client.GetStringAsync("/categories/Electronics?correlationId=3");

            // Assert - 3
            var expected3 =
@"Category: Electronics
Products: Laptops (3)";
            Assert.Equal(expected3, response4.Trim());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FormTagHelper_GeneratesExpectedContent(bool? optionsAntiForgery)
        {
            // Arrange
            var newServices = new ServiceCollection();
            newServices.ConfigureTagHelpers().ConfigureForm(options => options.GenerateAntiForgeryToken = optionsAntiForgery);
            var server = TestHelper.CreateServer(_app, SiteName, services => services.Add(newServices));
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

            // The K runtime compiles every file under compiler/resources as a resource at runtime with the same name
            // as the file name, in order to update a baseline you just need to change the file in that folder.
            var resourceName = string.Format(
                "compiler/resources/MvcTagHelpersWebSite.MvcTagHelper_Home.Form.Options.AntiForgery.{0}.html",
                optionsAntiForgery?.ToString() ?? "null"
            );
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync(resourceName);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/MvcTagHelper_Home/Form");
            var responseContent = await response.Content.ReadAsStringAsync();

            var forgeryTokens = AntiForgeryTestHelper.RetrieveAntiForgeryTokens(responseContent);
            expectedContent = string.Format(expectedContent, forgeryTokens.ToArray());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
            Assert.Equal(expectedContent.Trim(), responseContent.Trim());
        }
    }
}